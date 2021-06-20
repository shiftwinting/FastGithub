using DNS.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
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
        private readonly IOptions<DnsOptions> options;
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
            this.options = options;
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
            this.SetNameServers(IPAddress.Loopback, this.options.Value.UpStream);

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
            this.SetNameServers();

            return Task.CompletedTask;
        }

        /// <summary>
        /// 设备dns
        /// </summary>
        /// <param name="nameServers"></param>
        private void SetNameServers(params IPAddress[] nameServers)
        {
            var action = nameServers.Length == 0 ? "清除" : "设置";
            if (this.options.Value.SetToLocalMachine && OperatingSystem.IsWindows())
            {
                try
                {
                    NameServiceUtil.SetNameServers(nameServers);
                    this.logger.LogInformation($"{action}本机dns成功");
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning($"{action}本机dns失败：{ex.Message}");
                }
            }
        }
    }
}
