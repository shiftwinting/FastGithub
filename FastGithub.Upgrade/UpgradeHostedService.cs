using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Upgrade
{
    /// <summary>
    /// 升级后台服务
    /// </summary>
    sealed class UpgradeHostedService : BackgroundService
    {
        private readonly UpgradeService upgradeService;
        private readonly ILogger<UpgradeHostedService> logger;

        /// <summary>
        /// 升级后台服务
        /// </summary>
        /// <param name="logger"></param>
        public UpgradeHostedService(
            UpgradeService upgradeService,
            ILogger<UpgradeHostedService> logger)
        {
            this.upgradeService = upgradeService;
            this.logger = logger;
        }

        /// <summary>
        /// 检测版本
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
                this.logger.LogWarning($"升级失败：{ex.Message}");
            }
        }
    }
}
