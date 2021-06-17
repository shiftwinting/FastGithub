using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FastGithub.Scanner.Middlewares
{
    [Service(ServiceLifetime.Singleton)]
    sealed class StatisticsMiddleware : IMiddleware<GithubContext>
    {
        private readonly ILogger<StatisticsMiddleware> logger;

        public StatisticsMiddleware(ILogger<StatisticsMiddleware> logger)
        {
            this.logger = logger;
        }

        public async Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                await next();
            }
            finally
            {
                stopwatch.Stop();

                if (context.Available)
                {
                    context.History.AddSuccess(stopwatch.Elapsed);
                    this.logger.LogInformation(context.ToString());
                }
                else
                {
                    context.History.AddFailure(); 
                }
            }
        }
    }
}
