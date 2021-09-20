using FastGithub.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// app后台服务
    /// </summary>
    sealed class AppHostedService : BackgroundService
    {
        private readonly IOptions<FastGithubOptions> options;
        private readonly ILogger<AppHostedService> logger;

        public AppHostedService(
            IOptions<FastGithubOptions> options,
            ILogger<AppHostedService> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// 启动完成
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var version = ProductionVersion.Current;
            this.logger.LogInformation($"{nameof(FastGithub)}启动完成，当前版本为v{version}，访问 https://github.com/dotnetcore/fastgithub 关注新版本");
            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// 停止完成
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"{nameof(FastGithub)}已停止运行");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// 后台任务
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (OperatingSystem.IsWindows() == false)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), stoppingToken);
                if (await this.UseFastGithubProxyAsync() == false)
                {
                    var httpProxyPort = this.options.Value.HttpProxyPort;
                    this.logger.LogWarning($"请设置系统自动代理为http://{IPAddress.Loopback}:{httpProxyPort}，或手动代理http/https为{IPAddress.Loopback}:{httpProxyPort}");
                }
            }
        }

        /// <summary>
        /// 应用fastgithub代理
        /// </summary>
        /// <param name="proxyServer"></param>
        /// <param name="httpProxyPort"></param>
        /// <returns></returns>
        private async Task<bool> UseFastGithubProxyAsync()
        {
            var systemProxy = HttpClient.DefaultProxy;
            if (systemProxy == null)
            {
                return false;
            }

            var domain = this.options.Value.DomainConfigs.Keys.FirstOrDefault();
            if (domain == null)
            {
                return true;
            }

            var destination = new Uri($"https://{domain.Replace('*', 'a')}");
            var proxyServer = systemProxy.GetProxy(destination);
            if (proxyServer == null)
            {
                return false;
            }

            var httpProxyPort = this.options.Value.HttpProxyPort;
            if (proxyServer.Port != httpProxyPort)
            {
                return false;
            }

            if (IPAddress.TryParse(proxyServer.Host, out var address))
            {
                return address.Equals(IPAddress.Loopback);
            }

            try
            {
                var addresses = await System.Net.Dns.GetHostAddressesAsync(proxyServer.Host);
                return addresses.Contains(IPAddress.Loopback);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
