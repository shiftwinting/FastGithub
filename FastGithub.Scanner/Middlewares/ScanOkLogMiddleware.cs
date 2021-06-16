using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FastGithub.Scanner.Middlewares
{
    [Service(ServiceLifetime.Singleton)]
    sealed class ScanOkLogMiddleware : IMiddleware<GithubContext>
    {
        private readonly ILogger<ScanOkLogMiddleware> logger;

        private record ScanOk(string Domain, IPAddress Address, int TotalScanCount, double SuccessRate, TimeSpan AvgElapsed);

        public ScanOkLogMiddleware(ILogger<ScanOkLogMiddleware> logger)
        {
            this.logger = logger;
        }

        public async Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            await next();

            if (context.Available)
            {
                var mesage = new ScanOk(
                    context.Domain,
                    context.Address,
                    context.Statistics.TotalScanCount,
                    context.Statistics.GetSuccessRate(),
                    context.Statistics.GetAvgElapsed()
                    );

                this.logger.LogInformation(mesage.ToString());
            }
        }
    }
}
