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

            options.OnChange(opt => SystemDnsUtil.FlushResolverCache());
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

            const int STANDARD_DNS_PORT = 53;
            if (dnsPort == STANDARD_DNS_PORT)
            {
                try
                {
                    SystemDnsUtil.SetPrimitiveDns(IPAddress.Loopback);
                    SystemDnsUtil.FlushResolverCache();
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex.Message);
                }
            }
            else
            {
                this.logger.LogWarning($"由于使用了非标准DNS端口{dnsPort}，你需要将{nameof(FastGithub)}设置为标准DNS的上游");
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
            return this.dnsOverUdpServer.HandleAsync(stoppingToken);
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

            try
            {
                SystemDnsUtil.RemovePrimitiveDns(IPAddress.Loopback);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message);
            }
            finally
            {
                SystemDnsUtil.FlushResolverCache();
            }

            return base.StopAsync(cancellationToken);
        }
    }
}
