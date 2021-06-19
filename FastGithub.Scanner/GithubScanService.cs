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
        private readonly DomainAddressFacotry domainAddressFactory;
        private readonly GithubContextCollection scanResults;
        private readonly ILogger<GithubScanService> logger;

        private readonly InvokeDelegate<GithubContext> fullScanDelegate;
        private readonly InvokeDelegate<GithubContext> resultScanDelegate;

        /// <summary>
        /// github扫描服务
        /// </summary>
        /// <param name="domainAddressFactory"></param>
        /// <param name="scanResults"></param>
        /// <param name="appService"></param>
        /// <param name="logger"></param>
        public GithubScanService(
            DomainAddressFacotry domainAddressFactory,
            GithubContextCollection scanResults,
            IServiceProvider appService,
            ILogger<GithubScanService> logger)
        {
            this.domainAddressFactory = domainAddressFactory;
            this.scanResults = scanResults;
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
            var domainAddresses = await this.domainAddressFactory.CreateDomainAddressesAsync(cancellationToken);

            var scanTasks = domainAddresses
                .Select(item => new GithubContext(item.Domain, item.Address, cancellationToken))
                .Select(ctx => ScanAsync(ctx));

            var results = await Task.WhenAll(scanTasks);
            var successCount = results.Count(item => item);
            this.logger.LogInformation($"完整扫描结束，成功{successCount}条共{results.Length}条");


            async Task<bool> ScanAsync(GithubContext context)
            {
                await this.fullScanDelegate(context);
                if (context.Available == true)
                {
                    this.scanResults.Add(context);
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
                .OrderByDescending(item => item.History.AvailableRate)
                .ThenBy(item => item.History.AvgElapsed);

            foreach (var context in contexts)
            {
                await this.resultScanDelegate(context);
            }

            this.logger.LogInformation($"结果扫描结束，共扫描{results.Length}条记录");
        }
    }
}
