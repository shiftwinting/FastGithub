using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// 后台服务
    /// </summary>
    sealed class HostedService : IHostedService
    {
        private readonly ILogger<HostedService> logger;

        /// <summary>
        /// 后台服务
        /// </summary>
        /// <param name="upgradeService"></param>
        /// <param name="logger"></param>
        public HostedService(ILogger<HostedService> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 服务启动时
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"{nameof(FastGithub)}启动完成，访问http://127.0.0.1或本机其它任意ip可进入Dashboard");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 服务停止时
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
