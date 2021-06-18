using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner.ScanMiddlewares
{
    /// <summary>
    /// tcp扫描中间件
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class TcpScanMiddleware : IMiddleware<GithubContext>
    {
        private const int PORT = 443;
        private readonly TimeSpan cacheTimeSpan = TimeSpan.FromMinutes(20d);
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly IMemoryCache memoryCache;
        private readonly ILogger<TcpScanMiddleware> logger;

        /// <summary>
        /// tcp扫描中间件
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public TcpScanMiddleware(
            IOptionsMonitor<GithubOptions> options,
            IMemoryCache memoryCache,
            ILogger<TcpScanMiddleware> logger)
        {
            this.options = options;
            this.memoryCache = memoryCache;
            this.logger = logger;
        }

        /// <summary>
        /// tcp扫描
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            var key = $"tcp://{context.Address}";
            if (this.memoryCache.TryGetValue<bool>(key, out var available) == false)
            {
                available = await this.TcpScanAsync(context);
                this.memoryCache.Set(key, available, cacheTimeSpan);
            }

            if (available == true)
            {
                await next();
            }
            else
            {
                this.logger.LogTrace($"{context.Domain} {context.Address}的{PORT}端口未开放");
            }
        }


        /// <summary>
        /// tcp扫描
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<bool> TcpScanAsync(GithubContext context)
        {
            try
            {
                var timeout = this.options.CurrentValue.Scan.TcpScanTimeout;
                using var timeoutTokenSource = new CancellationTokenSource(timeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, context.CancellationToken);

                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(context.Address, PORT, linkedTokenSource.Token);
                return true;
            }
            catch (Exception)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                return false;
            }
        }
    }
}
