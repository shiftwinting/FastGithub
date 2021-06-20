using Microsoft.Extensions.DependencyInjection;
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
    sealed class UpgradeHostedService : IHostedService
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<UpgradeHostedService> logger;

        /// <summary>
        /// 升级后台服务
        /// </summary>
        /// <param name="serviceScopeFactory"></param>
        /// <param name="logger"></param>
        public UpgradeHostedService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<UpgradeHostedService> logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        /// <summary>
        /// 检测版本
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = this.serviceScopeFactory.CreateScope();
                var upgradeService = scope.ServiceProvider.GetRequiredService<UpgradeService>();
                await upgradeService.UpgradeAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"升级失败：{ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
