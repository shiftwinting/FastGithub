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
                    this.logger.LogWarning($"设置为本机主DNS为{IPAddress.Loopback}失败：{ex.Message}");
                }
            }
            else
            {
                this.logger.LogWarning($"不支持自动设置DNS，请根据你的系统平台情况修改主DNS为{IPAddress.Loopback}");
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
