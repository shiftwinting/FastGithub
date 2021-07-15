using FastGithub.Scanner;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// 扫描结果轮询扫描后台服务
    /// </summary>
    sealed class GithubResultScanHostedService : BackgroundService
    {
        private readonly GithubScanService githubScanService;
        private readonly IOptionsMonitor<GithubScanOptions> options;

        /// <summary>
        /// 扫描结果轮询扫描后台服务
        /// </summary>
        /// <param name="githubScanService"></param>
        /// <param name="options"></param>
        public GithubResultScanHostedService(
            GithubScanService githubScanService,
            IOptionsMonitor<GithubScanOptions> options)
        {
            this.githubScanService = githubScanService;
            this.options = options;
        }

        /// <summary>
        /// 后台轮询扫描
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                await Task.Delay(this.options.CurrentValue.ResultScanInterval, stoppingToken);
                await githubScanService.ScanResultAsync(stoppingToken);
            }
        }
    }
}
