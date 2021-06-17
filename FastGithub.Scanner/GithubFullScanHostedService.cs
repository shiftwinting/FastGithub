using FastGithub.Scanner;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class GithubFullScanHostedService : BackgroundService
    {
        private readonly GithubScanService githubScanService;
        private readonly IOptionsMonitor<GithubOptions> options;

        public GithubFullScanHostedService(
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
                await githubScanService.ScanAllAsync();
                await Task.Delay(this.options.CurrentValue.ScanAllInterval, stoppingToken);
            }
        }
    }
}
