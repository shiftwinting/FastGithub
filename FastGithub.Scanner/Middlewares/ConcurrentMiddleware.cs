using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner.Middlewares
{
    [Service(ServiceLifetime.Singleton)]
    sealed class ConcurrentMiddleware : IMiddleware<GithubContext>
    {
        private readonly SemaphoreSlim semaphoreSlim = new(Environment.ProcessorCount * 4);

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
