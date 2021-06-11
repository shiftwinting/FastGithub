using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Middlewares
{
    sealed class ConcurrentMiddleware : IGithubMiddleware
    {
        private readonly SemaphoreSlim semaphoreSlim = new(50);

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
