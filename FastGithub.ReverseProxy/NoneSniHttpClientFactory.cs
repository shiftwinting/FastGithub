using FastGithub.Scanner;
using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>
        /// 生命周期
        /// </summary>
        private readonly TimeSpan lifeTime = TimeSpan.FromMinutes(2d);

        /// <summary>
        /// 具有生命周期的httpHandler延时创建对象
        /// </summary>
        private Lazy<LifetimeHttpHandler> lifeTimeHttpHandlerLazy;

        /// <summary>
        /// HttpHandler清理器
        /// </summary>
        private readonly LifetimeHttpHandlerCleaner httpHandlerCleaner = new LifetimeHttpHandlerCleaner();


        /// <summary>
        /// 禁用tls sni的HttpClient工厂
        /// </summary>
        /// <param name="githubScanResults"></param>
        public NoneSniHttpClientFactory(IGithubScanResults githubScanResults)
        {
            this.githubScanResults = githubScanResults;
            this.lifeTimeHttpHandlerLazy = new Lazy<LifetimeHttpHandler>(this.CreateHttpHandler, true);
        }

        /// <summary>
        /// 创建HttpClient
        /// </summary>
        /// <returns></returns>
        public HttpMessageInvoker CreateHttpClient()
        {
            var handler = this.lifeTimeHttpHandlerLazy.Value;
            return new HttpMessageInvoker(handler, disposeHandler: false);
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
                ConnectCallback = async (ctx, ct) =>
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(ctx.DnsEndPoint, ct);
                    var stream = new NetworkStream(socket, ownsSocket: true);
                    if (ctx.InitialRequestMessage.Headers.Host == null)
                    {
                        return stream;
                    }

                    var sslStream = new SslStream(stream, leaveInnerStreamOpen: false, delegate { return true; });
                    await sslStream.AuthenticateAsClientAsync(string.Empty, null, false);
                    return sslStream;
                }
            };

            var dnsHandler = new GithubDnsHttpHandler(this.githubScanResults, noneSniHandler);
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
