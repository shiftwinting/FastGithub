using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner.ScanMiddlewares
{
    [Service(ServiceLifetime.Singleton)]
    sealed class TcpScanMiddleware : IMiddleware<GithubContext>
    {
        private const int PORT = 443;
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly ILogger<TcpScanMiddleware> logger;

        public TcpScanMiddleware(
            IOptionsMonitor<GithubOptions> options,
            ILogger<TcpScanMiddleware> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        public async Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var timeout = this.options.CurrentValue.Scan.TcpScanTimeout;
                using var cancellationTokenSource = new CancellationTokenSource(timeout);
                await socket.ConnectAsync(context.Address, PORT, cancellationTokenSource.Token);

                await next();
            }
            catch (Exception)
            {
                this.logger.LogTrace($"{context.Domain} {context.Address}的{PORT}端口未开放");
            }
        }
    }
}
