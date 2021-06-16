using FastGithub.Scanner.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    [Service(ServiceLifetime.Singleton, ServiceType = typeof(IGithubScanService))]
    sealed class GithubScanService : IGithubScanService
    {
        private readonly GithubMetaService metaService;
        private readonly ILogger<GithubScanService> logger;
        private readonly GithubContextHashSet results = new();

        private readonly InvokeDelegate<GithubContext> fullScanDelegate;
        private readonly InvokeDelegate<GithubContext> resultScanDelegate;

        public GithubScanService(
            GithubMetaService metaService,
            ILogger<GithubScanService> logger,
            IPipelineBuilder<GithubContext> pipelineBuilder)
        {
            this.metaService = metaService;
            this.logger = logger;

            this.fullScanDelegate = pipelineBuilder
                .New()
                .Use<ConcurrentMiddleware>()
                .Use<PortScanMiddleware>()
                .Use<HttpsScanMiddleware>()
                .Use<ScanOkLogMiddleware>()
                .Build();

            this.resultScanDelegate = pipelineBuilder
                .New()
                .Use<PortScanMiddleware>()
                .Use<HttpsScanMiddleware>()
                .Use<ScanOkLogMiddleware>()
                .Build();
        }

        public async Task ScanAllAsync(CancellationToken cancellationToken = default)
        {
            this.logger.LogInformation("完整扫描开始");
            var meta = await this.metaService.GetMetaAsync(cancellationToken);
            if (meta != null)
            {
                var scanTasks = meta.ToGithubContexts().Select(ctx => ScanAsync(ctx));
                await Task.WhenAll(scanTasks);
            }

            this.logger.LogInformation("完整扫描结束");

            async Task ScanAsync(GithubContext context)
            {
                await this.fullScanDelegate(context);
                if (context.HttpElapsed != null)
                {
                    lock (this.results.SyncRoot)
                    {
                        this.results.Add(context);
                    }
                }
            }
        }

        public async Task ScanResultAsync()
        {
            this.logger.LogInformation("结果扫描开始");
            GithubContext[] contexts;
            lock (this.results.SyncRoot)
            {
                contexts = this.results.ToArray();
            }

            foreach (var context in contexts)
            {
                context.HttpElapsed = null;
                await this.resultScanDelegate(context);
            }

            this.logger.LogInformation("结果扫描结束");
        }

        public IPAddress? FindFastAddress(string domain)
        {
            if (domain.Contains("github", StringComparison.OrdinalIgnoreCase))
            {
                lock (this.results.SyncRoot)
                {
                    return this.results
                        .Where(item => item.Domain == domain && item.HttpElapsed != null)
                        .OrderBy(item => item.HttpElapsed)
                        .Select(item => item.Address)
                        .FirstOrDefault();
                }
            }

            return default;
        }
    }
}
