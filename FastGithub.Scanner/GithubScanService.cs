using FastGithub.Scanner.ScanMiddlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        private readonly GithubScanResults scanResults;
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
            GithubScanResults scanResults,
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
        /// 快速扫描所有的ip
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> ScanFastAsync(CancellationToken cancellationToken)
        {
            if (RawSocketPing.IsSupported == false)
            {
                this.logger.LogWarning($"{Environment.OSVersion.Platform}不支持快速扫描功能");
                return false;
            }

            try
            {
                this.logger.LogInformation("快速扫描开始..");
                var domainAddresses = await this.lookupFactory.LookupAsync(cancellationToken);

                // ping快速过滤可用的ip
                var destAddresses = domainAddresses.Select(item => item.Address);
                var hashSet = await RawSocketPing.PingAsync(destAddresses, TimeSpan.FromSeconds(3d), cancellationToken);
                var results = domainAddresses.Where(item => hashSet.Contains(item.Address)).ToArray();
                this.logger.LogInformation($"快速扫描到{hashSet.Count}条ip，{results.Length}条域名ip记录");

                var successCount = await this.ScanAsync(results, cancellationToken);
                this.logger.LogInformation($"快速扫描结束，成功{successCount}条共{domainAddresses.Count()}条");
                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"快速扫描失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 扫描所有的ip
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ScanAllAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("完整扫描开始..");
            var domainAddresses = await this.lookupFactory.LookupAsync(cancellationToken);
            var successCount = await this.ScanAsync(domainAddresses, cancellationToken);
            this.logger.LogInformation($"完整扫描结束，成功{successCount}条共{domainAddresses.Count()}条");
        }

        /// <summary>
        /// 扫描记录
        /// </summary>
        /// <param name="domainAddresses"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<int> ScanAsync(IEnumerable<DomainAddress> domainAddresses, CancellationToken cancellationToken)
        {
            var scanTasks = domainAddresses
                .Select(item => new GithubContext(item.Domain, item.Address, cancellationToken))
                .Select(ctx => ScanAsync(ctx));

            var results = await Task.WhenAll(scanTasks);
            return results.Count(item => item);

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
                .ThenByDescending(item => item.AvailableRate)
                .ThenBy(item => item.AvgElapsed);

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
