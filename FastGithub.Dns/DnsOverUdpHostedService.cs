using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DnsOverUdpHostedService> logger;

        /// <summary>
        /// dns后台服务
        /// </summary>
        /// <param name="dnsOverUdpServer"></param>
        /// <param name="conflictValidators"></param>
        /// <param name="logger"></param>
        public DnsOverUdpHostedService(
            DnsOverUdpServer dnsOverUdpServer,
            IEnumerable<IConflictValidator> conflictValidators,
            ILogger<DnsOverUdpHostedService> logger)
        {
            this.dnsOverUdpServer = dnsOverUdpServer;
            this.conflictValidators = conflictValidators;
            this.logger = logger;         
        }

        /// <summary>
        /// 启动dns
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                const int DNS_PORT = 53;
                this.dnsOverUdpServer.Listen(IPAddress.Any, DNS_PORT);
                this.logger.LogInformation($"已监听udp端口{DNS_PORT}，DNS服务启动完成");
            }
            catch (Exception ex)
            {
                this.logger.LogError($"DNS服务启动失败：{ex.Message}{Environment.NewLine}请配置系统或浏览器使用{nameof(FastGithub)}的DoH：https://127.0.0.1/dns-query，或向系统hosts文件添加github相关域名的ip为127.0.0.1");
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
            return base.StopAsync(cancellationToken);
        }
    }
}
