using DNS.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    sealed class DnsHostedService : IHostedService
    {
        private readonly DnsServer dnsServer; 
        private readonly ILogger<DnsHostedService> logger;

        public DnsHostedService(
            GithubRequestResolver githubRequestResolver,
            IOptions<DnsOptions> options,
            ILogger<DnsHostedService> logger)
        {
            this.dnsServer = new DnsServer(githubRequestResolver, options.Value.UpStream); 
            this.logger = logger;
        } 

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.dnsServer.Listen();
            this.logger.LogInformation("dns服务启用成功");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.dnsServer.Dispose();
            this.logger.LogInformation("dns服务已终止");
            return Task.CompletedTask;
        }
    }
}
