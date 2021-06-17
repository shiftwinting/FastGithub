using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner.ScanMiddlewares
{
    [Service(ServiceLifetime.Singleton)]
    sealed class ConcurrentMiddleware : IMiddleware<GithubContext>
    {
        private readonly SemaphoreSlim semaphoreSlim;

        public ConcurrentMiddleware()
        {
            var currentCount = Environment.ProcessorCount * 4;
            this.semaphoreSlim = new SemaphoreSlim(currentCount, currentCount);
        }

        public async Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            try
            {
                await this.semaphoreSlim.WaitAsync();
                await next();
            }
            finally
            {
                this.semaphoreSlim.Release();
            }
        }
    }
}
