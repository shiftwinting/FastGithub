using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// Host服务
    /// </summary>
    sealed class HostedService : IHostedService
    {
        private readonly ILogger<HostedService> logger;

        /// <summary>
        /// Host服务
        /// </summary>
        /// <param name="logger"></param>
        public HostedService(ILogger<HostedService> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var localhost = "https://127.0.0.1";
            this.logger.LogInformation($"{nameof(FastGithub)}启动完成，访问{localhost}或本机任意ip可查看使用说明");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
