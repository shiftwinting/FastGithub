using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 适用于请求github的HttpClientHandler
    /// </summary>
    [Service(ServiceLifetime.Transient)]
    public class GithubHttpClientHanlder : DelegatingHandler
    {
        private readonly IGithubScanResults githubScanResults;
        private readonly ILogger<GithubHttpClientHanlder> logger;

        /// <summary>
        /// 请求github的HttpClientHandler
        /// </summary>
        /// <param name="githubScanResults"></param>
        /// <param name="logger"></param>
        public GithubHttpClientHanlder(
            IGithubScanResults githubScanResults,
            ILogger<GithubHttpClientHanlder> logger)
        {
            this.githubScanResults = githubScanResults;
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
        /// 查找最快的ip来发送消息
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (uri != null && uri.HostNameType == UriHostNameType.Dns)
            {
                var address = this.githubScanResults.FindBestAddress(uri.Host);
                if (address != null)
                {
                    this.logger.LogInformation($"使用{address} No SNI请求{uri.Host}");
                    var builder = new UriBuilder(uri)
                    {
                        Host = address.ToString()
                    };
                    request.RequestUri = builder.Uri;
                    request.Headers.Host = uri.Host;
                }
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
