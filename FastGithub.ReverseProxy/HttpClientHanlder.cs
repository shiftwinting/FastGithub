using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
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
        private readonly ILogger<HttpClientHanlder> logger;

        /// <summary>
        /// YARP的HttpClientHandler
        /// </summary>
        /// <param name="domainResolver"></param> 
        public HttpClientHanlder(
            DomainResolver domainResolver,
            ILogger<HttpClientHanlder> logger)
        {
            this.domainResolver = domainResolver;
            this.logger = logger;
            this.InnerHandler = CreateNoneSniHttpHandler();
        }

        /// <summary>
        /// 创建无Sni发送的httpHandler
        /// </summary>
        /// <returns></returns>
        private static HttpMessageHandler CreateNoneSniHttpHandler()
        {
            return new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false,
                AllowAutoRedirect = false,
                ConnectCallback = async (ctx, ct) =>
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(ctx.DnsEndPoint, ct);
                    var stream = new NetworkStream(socket, ownsSocket: true);
                    var sniContext = ctx.InitialRequestMessage.GetSniContext();
                    if (sniContext.IsHttps == false)
                    {
                        return stream;
                    }

                    var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        TargetHost = sniContext.TlsSniValue,
                        RemoteCertificateValidationCallback = delegate { return true; }
                    }, ct);
                    return sslStream;
                }
            };
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

                // 计算Sni
                var context = request.GetSniContext();
                if (context.IsHttps && context.TlsSni)
                {
                    context.TlsSniValue = uri.Host;
                    this.logger.LogInformation($"[{address}--Sni->{uri.Host}]");
                }
                else
                {
                    this.logger.LogInformation($"[{address}--NoSni->{uri.Host}]");
                }
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
