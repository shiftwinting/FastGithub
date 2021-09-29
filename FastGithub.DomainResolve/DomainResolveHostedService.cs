using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名解析后台服务
    /// </summary>
    sealed class DomainResolveHostedService : BackgroundService
    {
        private readonly DnscryptProxy dnscryptProxy;
        private readonly DnsClient dnsClient;
        private readonly TimeSpan pingPeriodTimeSpan = TimeSpan.FromSeconds(30d);

        /// <summary>
        /// 域名解析后台服务
        /// </summary>
        /// <param name="dnscryptProxy"></param>
        /// <param name="dnsClient"></param>
        public DomainResolveHostedService(
            DnscryptProxy dnscryptProxy,
            DnsClient dnsClient)
        {
            this.dnscryptProxy = dnscryptProxy;
            this.dnsClient = dnsClient;
        }

        /// <summary>
        /// 后台任务
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this.dnscryptProxy.StartAsync(stoppingToken);
            while (stoppingToken.IsCancellationRequested == false)
            {
                await this.dnsClient.PingAllDomainsAsync(stoppingToken);
                await Task.Delay(this.pingPeriodTimeSpan, stoppingToken);
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.dnscryptProxy.Stop();
            return base.StopAsync(cancellationToken);
        }
    }
}
