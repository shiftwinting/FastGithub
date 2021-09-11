using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
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
                var builder = new StringBuilder().AppendLine($"DNS服务启动失败({ex.Message})，你可以选择如下的一种操作：");
                builder.AppendLine($"1. 关闭占用udp53端口的进程然后重新打开本程序");
                builder.AppendLine($"2. 向系统hosts文件添加要加速的域名的ip为127.0.0.1");
                builder.AppendLine($"3. 配置系统或浏览器使用DNS over HTTPS：https://127.0.0.1/dns-query");
                builder.Append($"4. 在局域网其它设备上运行本程序，然后将本机DNS设置为局域网设备的IP");
                this.logger.LogError(builder.ToString());
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
