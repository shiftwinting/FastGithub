using FastGithub.Scanner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 禁用tls sni的HttpClient工厂
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class NoneSniHttpClientFactory
    {
        private readonly IGithubScanResults githubScanResults;
        private readonly IOptionsMonitor<GithubReverseProxyOptions> options;
        private readonly ILogger<NoneSniHttpClientFactory> logger;

        /// <summary>
        /// 生命周期
        /// </summary>
        private readonly TimeSpan lifeTime = TimeSpan.FromMinutes(2d);

        /// <summary>
        /// 具有生命周期的httpHandler延时创建对象
        /// </summary>
        private volatile Lazy<LifetimeHttpHandler> lifeTimeHttpHandlerLazy;

        /// <summary>
        /// HttpHandler清理器
        /// </summary>
        private readonly LifetimeHttpHandlerCleaner httpHandlerCleaner;


        /// <summary>
        /// 禁用tls sni的HttpClient工厂
        /// </summary>
        /// <param name="githubScanResults"></param>
        public NoneSniHttpClientFactory(
            IGithubScanResults githubScanResults,
            IOptionsMonitor<GithubReverseProxyOptions> options,
            ILogger<NoneSniHttpClientFactory> logger)
        {
            this.githubScanResults = githubScanResults;
            this.options = options;
            this.logger = logger;
            this.lifeTimeHttpHandlerLazy = new Lazy<LifetimeHttpHandler>(this.CreateHttpHandler, true);
            this.httpHandlerCleaner = new LifetimeHttpHandlerCleaner(logger);
        }

        /// <summary>
        /// 创建HttpClient
        /// </summary>
        /// <returns></returns>
        public HttpMessageInvoker CreateHttpClient()
        {
            var handler = this.lifeTimeHttpHandlerLazy.Value;
            return new HttpMessageInvoker(handler);
        }

        /// <summary>
        /// 创建具有生命周期控制的httpHandler
        /// </summary>
        /// <returns></returns>
        private LifetimeHttpHandler CreateHttpHandler()
        {
            var noneSniHandler = new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false,
                AllowAutoRedirect = false,
                MaxConnectionsPerServer = this.options.CurrentValue.MaxConnectionsPerServer,
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

            var dnsHandler = new GithubDnsHttpHandler(this.githubScanResults, noneSniHandler, this.logger);
            return new LifetimeHttpHandler(dnsHandler, this.lifeTime, this.OnHttpHandlerDeactivate);
        }

        /// <summary>
        /// 当有httpHandler失效时
        /// </summary>
        /// <param name="handler">httpHandler</param>
        private void OnHttpHandlerDeactivate(LifetimeHttpHandler handler)
        {
            // 切换激活状态的记录的实例
            this.lifeTimeHttpHandlerLazy = new Lazy<LifetimeHttpHandler>(this.CreateHttpHandler, true);
            this.httpHandlerCleaner.Add(handler);
        }
    }
}
