using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DnscryptProxy
{
    /// <summary>
    /// DnscryptProxy后台服务
    /// </summary>
    sealed class DnscryptProxyHostedService : IHostedService
    {
        private readonly DnscryptProxyService dnscryptProxyService;
        private readonly ILogger<DnscryptProxyHostedService> logger;
        private readonly TimeSpan dnsOKTimeout = TimeSpan.FromSeconds(60d);

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

                // 监听意外退出
                var process = this.dnscryptProxyService.Process;
                if (process == null)
                {
                    this.OnProcessExit(null, new EventArgs());
                }
                else
                {
                    process.EnableRaisingEvents = true;
                    process.Exited += this.OnProcessExit;
                    await this.WaitForDnsOKAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{this.dnscryptProxyService}启动失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 等待dns服务初始化
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task WaitForDnsOKAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"{this.dnscryptProxyService}正在初始化");
            using var timeoutTokenSource = new CancellationTokenSource(this.dnsOKTimeout);

            try
            {
                using var linkeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                await this.dnscryptProxyService.WaitForDnsOKAsync(linkeTokenSource.Token);
                this.logger.LogInformation($"{this.dnscryptProxyService}初始化完成");
            }
            catch (Exception)
            {
                if (timeoutTokenSource.IsCancellationRequested)
                {
                    this.logger.LogWarning($"{this.dnscryptProxyService}在{this.dnsOKTimeout.TotalSeconds}秒内未能初始化完成");
                }
            }
        }

        /// <summary>
        /// 进程退出时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnProcessExit(object? sender, EventArgs e)
        {
            if (this.dnscryptProxyService.ControllState != ControllState.Stopped)
            {
                this.logger.LogCritical($"{this.dnscryptProxyService}已意外停止，这将影响到{nameof(FastGithub)}解析域名的速度和准确性。");
            }
        }

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
