using FastGithub.Scanner;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class GithubFullScanHostedService : BackgroundService
    {
        private readonly IGithubScanService githubScanService;
        private readonly IOptionsMonitor<GithubOptions> options;

        public GithubFullScanHostedService(
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
                await githubScanService.ScanAllAsync(stoppingToken);
                await Task.Delay(this.options.CurrentValue.ScanAllInterval, stoppingToken);
            }
        }
    }
}
