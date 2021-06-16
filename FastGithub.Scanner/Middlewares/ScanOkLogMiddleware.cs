using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FastGithub.Scanner.Middlewares
{
    [Service(ServiceLifetime.Singleton)]
    sealed class ScanOkLogMiddleware : IMiddleware<GithubContext>
    {
        private readonly ILogger<ScanOkLogMiddleware> logger;

        public ScanOkLogMiddleware(ILogger<ScanOkLogMiddleware> logger)
        {
            this.logger = logger;
        }

        public Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            if (context.Available)
            {
                this.logger.LogInformation(context.ToString());
            }

            return next();
        }
    }
}
