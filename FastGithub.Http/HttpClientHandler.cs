using FastGithub.Configuration;
using FastGithub.DomainResolve;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Http
{
    /// <summary>
    /// HttpClientHandler
    /// </summary> 
    class HttpClientHandler : DelegatingHandler
    {
        private readonly DomainConfig domainConfig;
        private readonly IDomainResolver domainResolver;

        /// <summary>
        /// HttpClientHandler
        /// </summary>
        /// <param name="domainConfig"></param>
        /// <param name="domainResolver"></param>
        public HttpClientHandler(DomainConfig domainConfig, IDomainResolver domainResolver)
        {
            this.domainResolver = domainResolver;
            this.domainConfig = domainConfig;
            this.InnerHandler = this.CreateSocketsHttpHandler();
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                await this.ProcessRequestAsync(request, cancellationToken);
                return await this.SendRequestAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                this.InterceptRequestException(request, ex);
                throw;
            }
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ProcessRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (uri == null)
            {
                throw new FastGithubException("必须指定请求的URI");
            }

            // 请求上下文信息
            var context = new RequestContext
            {
                Domain = uri.Host,
                IsHttps = uri.Scheme == Uri.UriSchemeHttps,
                TlsSniValue = this.domainConfig.GetTlsSniPattern().WithDomain(uri.Host).WithRandom()
            };
            request.SetRequestContext(context);

            // 解析ip，替换https为http
            var uriBuilder = new UriBuilder(uri)
            {
                Scheme = Uri.UriSchemeHttp
            };

            if (uri.HostNameType == UriHostNameType.Dns)
            {
                if (IPAddress.TryParse(this.domainConfig.IPAddress, out var address) == false)
                {
                    var endPoint = new DnsEndPoint(uri.Host, uri.Port);
                    address = await this.domainResolver.ResolveAsync(endPoint, cancellationToken);
                }
                uriBuilder.Host = address.ToString();
                request.Headers.Host = context.Domain;
                context.TlsSniValue = context.TlsSniValue.WithIPAddress(address);
            }
            request.RequestUri = uriBuilder.Uri;
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (this.domainConfig.Timeout != null)
            {
                using var timeoutTokenSource = new CancellationTokenSource(this.domainConfig.Timeout.Value);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                return await base.SendAsync(request, linkedTokenSource.Token);
            }
            else
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }

        /// <summary>
        /// 拦截请求异常
        /// 查找TimedOut的ip地址添加到黑名单
        /// </summary>
        /// <param name="request"></param>
        /// <param name="exception"></param>
        private void InterceptRequestException(HttpRequestMessage request, HttpRequestException exception)
        {
            if (request.RequestUri == null || IsTimedOutSocketError(exception) == false)
            {
                return;
            }

            if (IPAddress.TryParse(request.RequestUri.Host, out var address))
            {
                this.domainResolver.SetDisabled(address);
            }

            if (request.Headers.Host != null)
            {
                this.domainResolver.FlushDomain(new DnsEndPoint(request.Headers.Host, request.RequestUri.Port));
            }


            static bool IsTimedOutSocketError(HttpRequestException exception)
            {
                var inner = exception.InnerException;
                while (inner != null)
                {
                    if (inner is SocketException socketException && socketException.SocketErrorCode == SocketError.TimedOut)
                    {
                        return true;
                    }
                    inner = inner.InnerException;
                }
                return false;
            }
        }

        /// <summary>
        /// 创建转发代理的httpHandler
        /// </summary>
        /// <returns></returns>
        private SocketsHttpHandler CreateSocketsHttpHandler()
        {
            return new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                    var stream = new NetworkStream(socket, ownsSocket: true);

                    var requestContext = context.InitialRequestMessage.GetRequestContext();
                    if (requestContext.IsHttps == false)
                    {
                        return stream;
                    }

                    var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        TargetHost = requestContext.TlsSniValue.Value,
                        RemoteCertificateValidationCallback = ValidateServerCertificate
                    }, cancellationToken);
                    return sslStream;


                    bool ValidateServerCertificate(object sender, X509Certificate? cert, X509Chain? chain, SslPolicyErrors errors)
                    {
                        if (errors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
                        {
                            if (this.domainConfig.TlsIgnoreNameMismatch == true)
                            {
                                return true;
                            }

                            var domain = requestContext.Domain;
                            var dnsNames = ReadDnsNames(cert);
                            return dnsNames.Any(dns => IsMatch(dns, domain));
                        }

                        return errors == SslPolicyErrors.None;
                    }
                }
            };
        }

        /// <summary>
        /// 读取使用的DNS名称
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        private static IEnumerable<string> ReadDnsNames(X509Certificate? cert)
        {
            if (cert == null)
            {
                yield break;
            }
            var parser = new Org.BouncyCastle.X509.X509CertificateParser();
            var x509Cert = parser.ReadCertificate(cert.GetRawCertData());
            var subjects = x509Cert.GetSubjectAlternativeNames();

            foreach (var subject in subjects)
            {
                if (subject is IList list)
                {
                    if (list.Count >= 2 && list[0] is int nameType && nameType == 2)
                    {
                        var dnsName = list[1]?.ToString();
                        if (dnsName != null)
                        {
                            yield return dnsName;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 比较域名
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        private static bool IsMatch(string dnsName, string? domain)
        {
            if (domain == null)
            {
                return false;
            }
            if (dnsName == domain)
            {
                return true;
            }
            if (dnsName[0] == '*')
            {
                return domain.EndsWith(dnsName[1..]);
            }
            return false;
        }
    }
}
