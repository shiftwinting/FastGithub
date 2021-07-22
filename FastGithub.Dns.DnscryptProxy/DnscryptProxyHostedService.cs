using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns.DnscryptProxy
{
    /// <summary>
    /// DnscryptProxy后台服务
    /// </summary>
    sealed class DnscryptProxyHostedService : IHostedService
    {
        private readonly DnscryptProxyService dnscryptProxyService;
        private readonly ILogger<DnscryptProxyHostedService> logger;

        /// <summary>
        /// DnscryptProxy后台服务
        /// </summary>
        /// <param name="dnscryptProxyService"></param>
        /// <param name="logger"></param>
        public DnscryptProxyHostedService(
            DnscryptProxyService dnscryptProxyService,
            ILogger<DnscryptProxyHostedService> logger)
        {
            this.dnscryptProxyService = dnscryptProxyService;
            this.logger = logger;
        }

        /// <summary>
        /// 启动dnscrypt-proxy
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this.dnscryptProxyService.StartAsync(cancellationToken);
                this.logger.LogInformation($"{this.dnscryptProxyService}启动成功");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{this.dnscryptProxyService}启动失败：{ex.Message}");
            }
        }

        ///// <summary>
        ///// 后台监控
        ///// </summary>
        ///// <param name="stoppingToken"></param>
        ///// <returns></returns>
        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    await Task.Yield();

        //    this.dnscryptProxyService.Process?.WaitForExit();
        //    if (this.isStopped == false)
        //    {
        //        this.logger.LogCritical($"{this.dnscryptProxyService}已停止运行，{nameof(FastGithub)}将无法解析域名。你可以把配置文件的{nameof(FastGithubOptions.PureDns)}修改为其它可用的DNS以临时使用。");
        //    }
        //}

        /// <summary>
        /// 停止dnscrypt-proxy
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.dnscryptProxyService.Stop();
                this.logger.LogInformation($"{this.dnscryptProxyService}已停止");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{this.dnscryptProxyService}停止失败：{ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
