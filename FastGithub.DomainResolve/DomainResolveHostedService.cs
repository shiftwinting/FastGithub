using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名解析后台服务
    /// </summary>
    sealed class DomainResolveHostedService : BackgroundService
    {
        private readonly DnscryptProxy dnscryptProxy;
        private readonly DomainSpeedTester speedTester;
        private readonly ILogger<DomainResolveHostedService> logger;

        private readonly TimeSpan speedTestDueTime = TimeSpan.FromSeconds(10d);
        private readonly TimeSpan speedTestPeriod = TimeSpan.FromMinutes(2d);

        /// <summary>
        /// 域名解析后台服务
        /// </summary>
        /// <param name="dnscryptProxy"></param>
        /// <param name="speedTester"></param>
        /// <param name="logger"></param>
        public DomainResolveHostedService(
            DnscryptProxy dnscryptProxy,
            DomainSpeedTester speedTester,
            ILogger<DomainResolveHostedService> logger)
        {
            this.dnscryptProxy = dnscryptProxy;
            this.speedTester = speedTester;
            this.logger = logger;
        }

        /// <summary>
        /// 停止时
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await this.speedTester.SaveDomainsAsync();
            this.dnscryptProxy.Stop();
            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// 后台任务
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await this.dnscryptProxy.StartAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{this.dnscryptProxy}启动失败：{ex.Message}");
            }

            await Task.Delay(this.speedTestDueTime, stoppingToken);
            await this.speedTester.LoadDomainsAsync(stoppingToken);
            while (stoppingToken.IsCancellationRequested == false)
            {
                await this.speedTester.TestSpeedAsync(stoppingToken);
                await Task.Delay(this.speedTestPeriod, stoppingToken);
            }
        }
    }
}
