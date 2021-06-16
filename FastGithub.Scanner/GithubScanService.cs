using FastGithub.Scanner.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    [Service(ServiceLifetime.Singleton)]
    sealed class GithubScanService
    {
        private readonly GithubMetaService metaService;
        private readonly ILogger<GithubScanService> logger;
        private readonly GithubContextCollection contextCollection;

        private readonly InvokeDelegate<GithubContext> fullScanDelegate;
        private readonly InvokeDelegate<GithubContext> resultScanDelegate;

        public GithubScanService(
            GithubMetaService metaService,
            GithubContextCollection contextCollection,
            IServiceProvider appService,
            ILogger<GithubScanService> logger)
        {
            this.metaService = metaService;
            this.contextCollection = contextCollection;
            this.logger = logger;
            ;
            this.fullScanDelegate = new PipelineBuilder<GithubContext>(appService, ctx => Task.CompletedTask)
                .Use<ConcurrentMiddleware>()
                .Use<ScanOkLogMiddleware>()
                .Use<StatisticsMiddleware>()
                .Use<PortScanMiddleware>()
                .Use<HttpsScanMiddleware>()
                .Build();

            this.resultScanDelegate = new PipelineBuilder<GithubContext>(appService, ctx => Task.CompletedTask)
                .Use<ScanOkLogMiddleware>()
                .Use<StatisticsMiddleware>()
                .Use<PortScanMiddleware>()
                .Use<HttpsScanMiddleware>()
                .Build();
        }

        public async Task ScanAllAsync(CancellationToken cancellationToken = default)
        {
            this.logger.LogInformation("完整扫描开始..");
            var meta = await this.metaService.GetMetaAsync(cancellationToken);
            if (meta != null)
            {
                var scanTasks = meta.ToGithubContexts().Select(ctx => ScanAsync(ctx));
                var results = await Task.WhenAll(scanTasks);
                var successCount = results.Count(item => item);
                this.logger.LogInformation($"完整扫描结束，成功{successCount}条共{results.Length}条");
            }

            async Task<bool> ScanAsync(GithubContext context)
            {
                await this.fullScanDelegate(context);
                if (context.Available == true)
                {
                    this.contextCollection.Add(context);
                }
                return context.Available;
            }
        }

        public async Task ScanResultAsync()
        {
            this.logger.LogInformation("结果扫描开始..");

            var contexts = this.contextCollection.ToArray();
            foreach (var context in contexts)
            {
                await this.resultScanDelegate(context);
            }

            this.logger.LogInformation($"结果扫描结束，共扫描{contexts.Length}条记录");
        }
    }
}
