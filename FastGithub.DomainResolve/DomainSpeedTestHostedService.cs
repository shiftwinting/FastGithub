using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名的IP测速后台服务
    /// </summary>
    sealed class DomainSpeedTestHostedService : BackgroundService
    {
        private readonly DomainSpeedTestService speedTestService;
        private readonly TimeSpan testDueTime = TimeSpan.FromMinutes(1d);

        /// <summary>
        /// 域名的IP测速后台服务
        /// </summary>
        /// <param name="speedTestService"></param>
        public DomainSpeedTestHostedService(DomainSpeedTestService speedTestService)
        {
            this.speedTestService = speedTestService;
        }

        /// <summary>
        /// 启动时
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.speedTestService.LoadDataAsync(cancellationToken);
            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// 停止时
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await this.speedTestService.SaveDataAsync();
            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// 后台测速
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                await this.speedTestService.TestSpeedAsync(stoppingToken);
                await Task.Delay(this.testDueTime, stoppingToken);
            }
        }
    }
}
