using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class GithubScanResultHostedService : BackgroundService
    {
        private readonly GithubScanService githubScanService;
        private readonly IOptionsMonitor<GithubOptions> options;

        public GithubScanResultHostedService(
            GithubScanService githubScanService,
            IOptionsMonitor<GithubOptions> options)
        {
            this.githubScanService = githubScanService;
            this.options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                await Task.Delay(this.options.CurrentValue.ScanResultInterval, stoppingToken);
                await githubScanService.ScanResultAsync();
            }
        }
    }
}
