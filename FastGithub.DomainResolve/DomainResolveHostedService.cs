using Microsoft.Extensions.Hosting;
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

        private readonly TimeSpan speedTestDueTime = TimeSpan.FromSeconds(10d);
        private readonly TimeSpan speedTestPeriod = TimeSpan.FromMinutes(2d);

        /// <summary>
        /// 域名解析后台服务
        /// </summary>
        /// <param name="dnscryptProxy"></param>
        /// <param name="speedTester"></param> 
        public DomainResolveHostedService(
            DnscryptProxy dnscryptProxy,
            DomainSpeedTester speedTester)
        {
            this.dnscryptProxy = dnscryptProxy;
            this.speedTester = speedTester;
        }

        /// <summary>
        /// 后台任务
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this.dnscryptProxy.StartAsync(stoppingToken);
            await Task.Delay(this.speedTestDueTime, stoppingToken);

            while (stoppingToken.IsCancellationRequested == false)
            {
                await this.speedTester.TestSpeedAsync(stoppingToken);
                await Task.Delay(this.speedTestPeriod, stoppingToken);
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.dnscryptProxy.Stop();
            return base.StopAsync(cancellationToken);
        }
    }
}
