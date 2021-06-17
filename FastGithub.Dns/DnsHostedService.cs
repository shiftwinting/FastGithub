using DNS.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// dns后台服务
    /// </summary>
    sealed class DnsHostedService : IHostedService
    {
        private readonly DnsServer dnsServer;
        private readonly ILogger<DnsHostedService> logger;

        /// <summary>
        /// dns后台服务
        /// </summary>
        /// <param name="githubRequestResolver"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public DnsHostedService(
            GithubRequestResolver githubRequestResolver,
            IOptions<DnsOptions> options,
            ILogger<DnsHostedService> logger)
        {
            this.dnsServer = new DnsServer(githubRequestResolver, options.Value.UpStream);
            this.logger = logger;
        }

        /// <summary>
        /// 启动dns服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.dnsServer.Listen();
            this.logger.LogInformation("dns服务启用成功");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止dns服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.dnsServer.Dispose();
            this.logger.LogInformation("dns服务已终止");
            return Task.CompletedTask;
        }
    }
}
