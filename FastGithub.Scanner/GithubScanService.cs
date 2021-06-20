using FastGithub.Scanner.ScanMiddlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    /// <summary>
    /// github扫描服务
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class GithubScanService
    {
        private readonly GithubLookupFacotry lookupFactory;
        private readonly GithubContextCollection scanResults;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<GithubScanService> logger;

        private readonly InvokeDelegate<GithubContext> fullScanDelegate;
        private readonly InvokeDelegate<GithubContext> resultScanDelegate;

        /// <summary>
        /// github扫描服务
        /// </summary>
        /// <param name="lookupFactory"></param>
        /// <param name="scanResults"></param>
        /// <param name="appService"></param>
        /// <param name="logger"></param>
        public GithubScanService(
            GithubLookupFacotry lookupFactory,
            GithubContextCollection scanResults,
            IServiceProvider appService,
            ILoggerFactory loggerFactory,
            ILogger<GithubScanService> logger)
        {
            this.lookupFactory = lookupFactory;
            this.scanResults = scanResults;
            this.loggerFactory = loggerFactory;
            this.logger = logger;

            this.fullScanDelegate = new PipelineBuilder<GithubContext>(appService, ctx => Task.CompletedTask)
                .Use<ConcurrentMiddleware>()
                .Use<StatisticsMiddleware>()
                .Use<TcpScanMiddleware>()
                .Use<HttpsScanMiddleware>()
                .Build();

            this.resultScanDelegate = new PipelineBuilder<GithubContext>(appService, ctx => Task.CompletedTask)
                .Use<StatisticsMiddleware>()
                .Use<HttpsScanMiddleware>()
                .Build();
        }

        /// <summary>
        /// 扫描所有的ip
        /// </summary>
        /// <returns></returns>
        public async Task ScanAllAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("完整扫描开始..");
            var domainAddresses = await this.lookupFactory.LookupAsync(cancellationToken);

            var scanTasks = domainAddresses
                .Select(item => new GithubContext(item.Domain, item.Address, cancellationToken))
                .Select(ctx => ScanAsync(ctx));

            var results = await Task.WhenAll(scanTasks);
            var successCount = results.Count(item => item);
            this.logger.LogInformation($"完整扫描结束，成功{successCount}条共{results.Length}条");


            async Task<bool> ScanAsync(GithubContext context)
            {
                await this.fullScanDelegate(context);
                if (context.Available && this.scanResults.Add(context))
                {
                    this.logger.LogInformation($"扫描到{context}");
                }
                return context.Available;
            }
        }

        /// <summary>
        /// 扫描曾经扫描到的结果
        /// </summary>
        /// <returns></returns>
        public async Task ScanResultAsync()
        {
            this.logger.LogInformation("结果扫描开始..");

            var results = this.scanResults.ToArray();
            var contexts = results
                .OrderBy(item => item.Domain)
                .ThenByDescending(item => item.History.AvailableRate)
                .ThenBy(item => item.History.AvgElapsed);

            foreach (var context in contexts)
            {
                await this.resultScanDelegate(context);
                var domainLogger = this.loggerFactory.CreateLogger(context.Domain);
                if (context.Available == true)
                {
                    domainLogger.LogInformation(context.ToStatisticsString());
                }
                else
                {
                    domainLogger.LogWarning(context.ToStatisticsString());
                }
            }

            this.logger.LogInformation($"结果扫描结束，共扫描{results.Length}条记录");
        }
    }
}
