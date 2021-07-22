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
            var maxTryCount = 5;
            for (var i = 1; i <= maxTryCount; i++)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2d), stoppingToken);
                    await this.upgradeService.UpgradeAsync(stoppingToken);
                    break;
                }
                catch (Exception ex)
                {
                    if (i == maxTryCount)
                    {
                        this.logger.LogWarning($"升级失败：{ex.Message}");
                    }
                }
            }
        }
    }
}
