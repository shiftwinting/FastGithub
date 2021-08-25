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
    sealed class DnsOverUdpHostedService : BackgroundService
    {
        private readonly DnsOverUdpServer dnsOverUdpServer;
        private readonly IEnumerable<IConflictValidator> conflictValidators;
        private readonly IOptionsMonitor<FastGithubOptions> options;
        private readonly ILogger<DnsOverUdpHostedService> logger;

        /// <summary>
        /// dns后台服务
        /// </summary>
        /// <param name="dnsOverUdpServer"></param>
        /// <param name="conflictValidators"></param>
        /// <param name="options"></param> 
        /// <param name="logger"></param>
        public DnsOverUdpHostedService(
            DnsOverUdpServer dnsOverUdpServer,
            IEnumerable<IConflictValidator> conflictValidators,
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<DnsOverUdpHostedService> logger)
        {
            this.dnsOverUdpServer = dnsOverUdpServer;
            this.conflictValidators = conflictValidators;
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
            var dnsPort = this.options.CurrentValue.Listen.DnsPort;
            this.dnsOverUdpServer.Bind(IPAddress.Any, dnsPort);
            this.logger.LogInformation("DNS服务启动成功");

            const int DNS_PORT = 53;
            if (dnsPort != DNS_PORT)
            {
                this.logger.LogWarning($"由于使用了非标准DNS端口{dnsPort}，你需要将{nameof(FastGithub)}设置为标准DNS的上游");
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

            foreach (var item in this.conflictValidators)
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
            return this.dnsOverUdpServer.ListenAsync(stoppingToken);
        }

        /// <summary>
        /// 停止dns服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.dnsOverUdpServer.Dispose();
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
