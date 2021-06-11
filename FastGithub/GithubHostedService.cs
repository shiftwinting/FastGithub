using FastGithub.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class GithubHostedService : BackgroundService
    {
        private readonly GithubDelegate githubDelegate;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<GithubHostedService> logger;

        public GithubHostedService(
            IServiceProvider appServiceProvider,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<GithubHostedService> logger)
        {
            this.githubDelegate = new GithubBuilder(appServiceProvider, ctx => Task.CompletedTask)
                .Use<ConcurrentMiddleware>()
                .Use<PortScanMiddleware>()
                .Use<HttpTestMiddleware>()
                .Build();

            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = this.serviceScopeFactory.CreateScope();
            var metaService = scope.ServiceProvider.GetRequiredService<MetaService>();

            var meta = await metaService.GetMetaAsync();

            if (meta != null)
            {
                var contexts = new List<GithubContext>();
                var tasks = this.GetScanTasks(meta, contexts);
                await Task.WhenAll(tasks);

                var orderByContexts = contexts
                    .Where(item => item.HttpElapsed != null)
                    .OrderBy(item => item.HttpElapsed);

                foreach (var context in orderByContexts)
                {
                    this.logger.LogInformation($"{context.Address} {context.HttpElapsed}");
                }
            }

            this.logger.LogInformation("扫描结束");
        } 


        private IEnumerable<Task> GetScanTasks(Meta meta, IList<GithubContext> contexts)
        {
            foreach (var address in meta.ToIPv4Address())
            {
                var context = new GithubContext
                {
                    Address = address,
                };
                contexts.Add(context);
                yield return this.githubDelegate(context);
            }
        }
    }
}
