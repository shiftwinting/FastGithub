using FastGithub.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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
        private readonly IEnumerable<IDnsValidator> dnsValidators;
        private readonly IOptionsMonitor<FastGithubOptions> options;
        private readonly ILogger<DnsHostedService> logger;

        /// <summary>
        /// dns后台服务
        /// </summary>
        /// <param name="dnsServer"></param>
        /// <param name="dnsValidators"></param>
        /// <param name="options"></param> 
        /// <param name="logger"></param>
        public DnsHostedService(
            DnsServer dnsServer,
            IEnumerable<IDnsValidator> dnsValidators,
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<DnsHostedService> logger)
        {
            this.dnsServer = dnsServer;
            this.dnsValidators = dnsValidators;
            this.options = options;
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
            var port = this.options.CurrentValue.Listen.DnsPort;
            this.dnsServer.Bind(IPAddress.Any, port);
            this.logger.LogInformation("DNS服务启动成功");

            const int DNS_PORT = 53;
            if (port != DNS_PORT)
            {
                this.logger.LogWarning($"由于使用了非标准DNS端口{port}，你需要将{nameof(FastGithub)}设置为标准DNS的上游");
            }
            else if (OperatingSystem.IsWindows())
            {
                try
                {
                    SystemDnsUtil.DnsSetPrimitive(IPAddress.Loopback);
                    SystemDnsUtil.DnsFlushResolverCache();
                    this.logger.LogInformation($"设置成本机主DNS成功");
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning($"设置成本机主DNS为{IPAddress.Loopback}失败：{ex.Message}");
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                this.logger.LogWarning($"不支持自动设置本机DNS，手工添加{IPAddress.Loopback}做为/etc/resolv.conf的第一条记录");
            }
            else
            {
                this.logger.LogWarning($"不支持自动设置本机DNS，请手工添加{IPAddress.Loopback}做为连接网络的DNS的第一条记录");
            }

            foreach (var item in this.dnsValidators)
            {
                await item.ValidateAsync();
            }

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
