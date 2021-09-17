using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class AppHostedService : IHostedService
    {
        private readonly ILogger<AppHostedService> logger;

        public AppHostedService(ILogger<AppHostedService> logger)
        {
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var version = ProductionVersion.Current;
            this.logger.LogInformation($"{nameof(FastGithub)}启动完成，当前版本为v{version}，访问https://github.com/dotnetcore/FastGithub关注新版本");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"{nameof(FastGithub)}已停止运行");
            return Task.CompletedTask;
        }
    }
}
