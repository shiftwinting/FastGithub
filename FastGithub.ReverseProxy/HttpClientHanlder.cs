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

                    var tlsSniContext = context.InitialRequestMessage.GetTlsSniContext();
                    if (tlsSniContext.IsHttps == false)
                    {
                        return stream;
                    }

                    var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        TargetHost = tlsSniContext.TlsSniPattern.Value,
                        RemoteCertificateValidationCallback = delegate { return true; }
                    }, cancellationToken);
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

                var context = request.GetTlsSniContext();
                context.TlsSniPattern = context.TlsSniPattern.WithDomain(uri.Host).WithIPAddress(address).WithRandom();
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
