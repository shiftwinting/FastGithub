using FastGithub.Upgrade;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// Host服务
    /// </summary>
    sealed class HostedService : BackgroundService
    {
        private readonly UpgradeService upgradeService;
        private readonly ILogger<HostedService> logger;

        /// <summary>
        /// Host服务
        /// </summary>
        /// <param name="upgradeService"></param>
        /// <param name="logger"></param>
        public HostedService(
            UpgradeService upgradeService,
            ILogger<HostedService> logger)
        {
            this.upgradeService = upgradeService;
            this.logger = logger;
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
                await this.upgradeService.UpgradeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"升级检测失败：{ex.Message}");
            }
            finally
            {
                this.logger.LogInformation($"{nameof(FastGithub)}启动完成，访问https://127.0.0.1或本机其它任意ip可进入Dashboard");
            }
        }
    }
}
