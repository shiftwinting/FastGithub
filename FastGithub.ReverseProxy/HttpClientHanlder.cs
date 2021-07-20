using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// YARP的HttpClientHandler
    /// </summary> 
    class HttpClientHanlder : DelegatingHandler
    {
        private readonly DomainResolver domainResolver;

        /// <summary>
        /// YARP的HttpClientHandler
        /// </summary>
        /// <param name="domainResolver"></param> 
        public HttpClientHanlder(DomainResolver domainResolver)
        {
            this.domainResolver = domainResolver;
            this.InnerHandler = CreateSocketsHttpHandler();
        }

        /// <summary>
        /// 创建转发代理的httpHandler
        /// </summary>
        /// <returns></returns>
        private static SocketsHttpHandler CreateSocketsHttpHandler()
        {
            return new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false,
                AllowAutoRedirect = false,
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
                        TargetHost = requestContext.TlsSniPattern.Value,
                        RemoteCertificateValidationCallback = ValidateServerCertificate
                    }, cancellationToken);
                    return sslStream;


                    bool ValidateServerCertificate(object sender, X509Certificate? cert, X509Chain? chain, SslPolicyErrors errors)
                    {
                        if (errors == SslPolicyErrors.RemoteCertificateNameMismatch)
                        {
                            var host = requestContext.Host;
                            var dnsNames = ReadDnsNames(cert);
                            return dnsNames.Any(dns => IsMatch(dns, host));
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
                    var type = (int)list[0]!;
                    if (type == 2) // DNS
                    {
                        yield return list[list.Count - 1]!.ToString()!;
                    }
                }
            }
        }

        /// <summary>
        /// 比较域名
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private static bool IsMatch(string dnsName, string? host)
        {
            if (host == null)
            {
                return false;
            }
            if (dnsName == host)
            {
                return true;
            }
            if (dnsName[0] == '*')
            {
                return host.EndsWith(dnsName[1..]);
            }
            return false;
        }

        /// <summary>
        /// 替换域名为ip
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (uri != null && uri.HostNameType == UriHostNameType.Dns)
            {
                var address = await this.domainResolver.ResolveAsync(uri.Host, cancellationToken);
                var builder = new UriBuilder(uri)
                {
                    Scheme = Uri.UriSchemeHttp,
                    Host = address.ToString(),
                };
                request.RequestUri = builder.Uri;
                request.Headers.Host = uri.Host;

                var context = request.GetRequestContext();
                context.TlsSniPattern = context.TlsSniPattern.WithDomain(uri.Host).WithIPAddress(address).WithRandom();
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
