using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// dns后台服务
    /// </summary>
    sealed class DnsHostedService : BackgroundService
    {
        private readonly DnsServer dnsServer;
        private readonly HostsFileValidator hostsValidator;
        private readonly ILogger<DnsHostedService> logger;

        /// <summary>
        /// dns后台服务
        /// </summary>
        /// <param name="dnsServer"></param>
        /// <param name="hostsValidator"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public DnsHostedService(
            DnsServer dnsServer,
            HostsFileValidator hostsValidator,
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<DnsHostedService> logger)
        {
            this.dnsServer = dnsServer;
            this.hostsValidator = hostsValidator;
            this.logger = logger;

            options.OnChange(opt =>
            {
                if (OperatingSystem.IsWindows())
                {
                    SystemDnsUtil.DnsFlushResolverCache();
                }
            });
        }

        /// <summary>
        /// 启动dns
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            this.dnsServer.Bind(IPAddress.Any, 53);
            this.logger.LogInformation("DNS服务启动成功");

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    SystemDnsUtil.DnsSetPrimitive(IPAddress.Loopback);
                    SystemDnsUtil.DnsFlushResolverCache();
                    this.logger.LogInformation($"设置为本机主DNS成功");
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning($"设置为本机主DNS失败：{ex.Message}");
                }
            }
            else
            {
                this.logger.LogWarning("平台不支持自动设置DNS，请手动设置网卡的主DNS为127.0.0.1");
            }

            await this.hostsValidator.ValidateAsync();
            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// dns后台
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return this.dnsServer.ListenAsync(stoppingToken);
        }

        /// <summary>
        /// 停止dns服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.dnsServer.Dispose();
            this.logger.LogInformation("DNS服务已停止");

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    SystemDnsUtil.DnsFlushResolverCache();
                    SystemDnsUtil.DnsRemovePrimitive(IPAddress.Loopback);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning($"恢复DNS记录失败：{ex.Message}");
                }
            }

            return base.StopAsync(cancellationToken);
        }
    }
}
