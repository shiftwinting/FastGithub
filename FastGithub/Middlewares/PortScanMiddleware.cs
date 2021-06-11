using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Middlewares
{
    sealed class PortScanMiddleware : IGithubMiddleware
    {
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(1d);
        private readonly ILogger<PortScanMiddleware> logger;

        public PortScanMiddleware(ILogger<PortScanMiddleware> logger)
        {
            this.logger = logger;
        }

        public async Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            try
            { 
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                using var cancellationTokenSource = new CancellationTokenSource(this.timeout);
                await socket.ConnectAsync(context.Address, 443, cancellationTokenSource.Token);

                await next();
            }
            catch (Exception)
            {
                this.logger.LogInformation($"{context.Address}的443端口未开放");
            }
        }
    }
}
