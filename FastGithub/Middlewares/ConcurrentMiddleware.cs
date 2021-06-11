using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Middlewares
{
    sealed class ConcurrentMiddleware : IGithubMiddleware
    {
        private readonly SemaphoreSlim semaphoreSlim;

        public ConcurrentMiddleware(IOptions<GithubOptions> options)
        {
            this.semaphoreSlim = new SemaphoreSlim(options.Value.Concurrent);
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
