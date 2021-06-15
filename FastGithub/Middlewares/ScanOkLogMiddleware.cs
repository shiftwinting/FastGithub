using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FastGithub.Middlewares
{
    sealed class ScanOkLogMiddleware : IGithubScanMiddleware
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
                this.logger.LogInformation(context.ToString());
            }

            return next();
        }
    }
}
