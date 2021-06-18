using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FastGithub.Scanner.ScanMiddlewares
{
    /// <summary>
    /// 扫描统计中间件
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class StatisticsMiddleware : IMiddleware<GithubContext>
    {
        private readonly ILogger<StatisticsMiddleware> logger;

        /// <summary>
        /// 扫描统计中间件
        /// </summary>
        /// <param name="logger"></param>
        public StatisticsMiddleware(ILogger<StatisticsMiddleware> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 记录扫描结果
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
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

                if (context.CancellationToken.IsCancellationRequested == false)
                {
                    context.History.Add(context.Available, stopwatch.Elapsed);
                    if (context.History.AvailableRate > 0d)
                    {
                        this.logger.LogInformation(context.ToString());
                    }
                }
            }
        }
    }
}
