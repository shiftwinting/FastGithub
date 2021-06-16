using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FastGithub.Scanner.Middlewares
{
    [Service(ServiceLifetime.Singleton)]
    sealed class StatisticsMiddleware : IMiddleware<GithubContext>
    {
        public async Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                await next();
            }
            finally
            {
                stopwatch.Stop();
                if (context.Available)
                {
                    context.Statistics.SetScanSuccess(stopwatch.Elapsed);
                }
                else
                {
                    context.Statistics.SetScanFailure();
                }
            }
        }
    }
}
