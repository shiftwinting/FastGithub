using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// DnscryptProxy后台服务
    /// </summary>
    sealed class DnscryptProxyHostedService : BackgroundService
    {
        private readonly ILogger<DnscryptProxyHostedService> logger;
        private readonly DnscryptProxy dnscryptProxy;

        /// <summary>
        /// DnscryptProxy后台服务
        /// </summary>
        /// <param name="dnscryptProxy"></param>
        /// <param name="logger"></param>
        public DnscryptProxyHostedService(
            DnscryptProxy dnscryptProxy,
            ILogger<DnscryptProxyHostedService> logger)
        {
            this.dnscryptProxy = dnscryptProxy;
            this.logger = logger;
        }

        /// <summary>
        /// 启动dnscrypt-proxy
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
        }

        /// <summary>
        /// 停止dnscrypt-proxy
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.dnscryptProxy.Stop();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{this.dnscryptProxy}停止失败：{ex.Message}");
            }
            return base.StopAsync(cancellationToken);
        }
    }
}
