using System;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 适用于请求github的HttpClientHandler
    /// </summary> 
    class GithubHttpClientHanlder : DelegatingHandler
    {
        private readonly GithubResolver githubResolver;

        /// <summary>
        /// 请求github的HttpClientHandler
        /// </summary>
        /// <param name="githubResolver"></param> 
        public GithubHttpClientHanlder(GithubResolver githubResolver)
        {
            this.githubResolver = githubResolver;
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
                    if (ctx.InitialRequestMessage.Headers.Host == null)
                    {
                        return stream;
                    }

                    var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        TargetHost = string.Empty,
                        RemoteCertificateValidationCallback = delegate { return true; }
                    }, ct);
                    return sslStream;
                }
            };
        }


        /// <summary>
        /// 替换github域名为ip
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (uri != null && uri.HostNameType == UriHostNameType.Dns)
            {
                var address = await this.githubResolver.ResolveAsync(uri.Host, cancellationToken);
                var builder = new UriBuilder(uri)
                {
                    Scheme = Uri.UriSchemeHttp,
                    Host = address.ToString(),
                    Port = 443
                };
                request.RequestUri = builder.Uri;
                request.Headers.Host = uri.Host;
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
