using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner.ScanMiddlewares
{
    /// <summary>
    /// 扫描并发限制中间件
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class ConcurrentMiddleware : IMiddleware<GithubContext>
    {
        private readonly SemaphoreSlim semaphoreSlim;

        /// <summary>
        /// 扫描并发限制中间件
        /// </summary>
        public ConcurrentMiddleware()
        {
            var currentCount = Environment.ProcessorCount * 2;
            this.semaphoreSlim = new SemaphoreSlim(currentCount, currentCount);
        }

        /// <summary>
        /// 限制描并发扫
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            try
            {
                await this.semaphoreSlim.WaitAsync(context.CancellationToken);
                await next();
            }
            finally
            {
                this.semaphoreSlim.Release();
            }
        }
    }
}
