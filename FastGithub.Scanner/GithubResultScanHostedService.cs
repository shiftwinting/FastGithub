using FastGithub.Scanner;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class GithubResultScanHostedService : BackgroundService
    {
        private readonly IGithubScanService githubScanService;
        private readonly IOptionsMonitor<GithubOptions> options;

        public GithubResultScanHostedService(
            IGithubScanService githubScanService,
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
