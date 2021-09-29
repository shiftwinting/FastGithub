using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名解析后台服务
    /// </summary>
    sealed class DnscryptProxyHostedService : BackgroundService
    {
        private readonly DnscryptProxy dnscryptProxy;

        /// <summary>
        /// 域名解析后台服务
        /// </summary>
        /// <param name="dnscryptProxy"></param> 
        public DnscryptProxyHostedService(DnscryptProxy dnscryptProxy)
        {
            this.dnscryptProxy = dnscryptProxy;
        }

        /// <summary>
        /// 后台任务
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this.dnscryptProxy.StartAsync(stoppingToken);
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
