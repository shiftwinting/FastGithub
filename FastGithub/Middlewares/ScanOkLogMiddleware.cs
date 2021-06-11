using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FastGithub.Middlewares
{
    sealed class ScanOkLogMiddleware : IGithubMiddleware
    {
        private readonly ILogger<ScanOkLogMiddleware> logger;

        public ScanOkLogMiddleware(ILogger<ScanOkLogMiddleware> logger)
        {
            this.logger = logger;
        }

        public Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            if (context.HttpElapsed != null)
            {
                this.logger.LogWarning(context.ToString());
            }

            return next();
        }
    }
}
