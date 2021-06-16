using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner.Middlewares
{
    [Service(ServiceLifetime.Singleton)]
    sealed class PortScanMiddleware : IMiddleware<GithubContext>
    {
        private const int PORT = 443;
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly ILogger<PortScanMiddleware> logger;

        public PortScanMiddleware(
            IOptionsMonitor<GithubOptions> options,
            ILogger<PortScanMiddleware> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        public async Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                using var cancellationTokenSource = new CancellationTokenSource(this.options.CurrentValue.PortScanTimeout);
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
