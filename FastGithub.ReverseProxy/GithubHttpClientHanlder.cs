using FastGithub.Scanner;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 使用于请求github的HttpClientHandler
    /// </summary>
    [Service(ServiceLifetime.Transient)]
    sealed class GithubHttpClientHanlder : DelegatingHandler
    {
        private readonly IGithubScanResults githubScanResults;
        private readonly ILogger<GithubHttpClientHanlder> logger;
        private readonly IMemoryCache memoryCache;

        /// <summary>
        /// 请求github的HttpClientHandler
        /// </summary>
        /// <param name="githubScanResults"></param>
        /// <param name="logger"></param>
        public GithubHttpClientHanlder(
            IGithubScanResults githubScanResults,
            ILogger<GithubHttpClientHanlder> logger,
            IMemoryCache memoryCache)
        {
            this.githubScanResults = githubScanResults;
            this.logger = logger;
            this.memoryCache = memoryCache;
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
                var address = this.Resolve(uri.Host);
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

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        private IPAddress? Resolve(string domain)
        {
            return this.memoryCache.GetOrCreate(typeof(GithubHttpClientHanlder), e =>
            {
                e.SetAbsoluteExpiration(TimeSpan.FromSeconds(1d));
                return this.githubScanResults.FindBestAddress(domain);
            });
        }
    }
}
