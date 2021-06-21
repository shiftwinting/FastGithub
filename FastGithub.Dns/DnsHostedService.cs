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
        private IPAddress[]? dnsAddresses;

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
            this.dnsAddresses = this.SetNameServers(IPAddress.Loopback, this.options.Value.UpStream);

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

            if (this.dnsAddresses != null)
            {
                this.SetNameServers(this.dnsAddresses);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 设置dns
        /// </summary>
        /// <param name="nameServers"></param>
        /// <returns></returns>
        private IPAddress[]? SetNameServers(params IPAddress[] nameServers)
        {
            if (this.options.Value.SetToLocalMachine && OperatingSystem.IsWindows())
            {
                try
                {
                    var results = NameServiceUtil.SetNameServers(nameServers);
                    this.logger.LogInformation($"设置本机dns成功");
                    return results;
                }

                catch (Exception ex)
                {
                    this.logger.LogWarning($"设置本机dns失败：{ex.Message}");
                }
            }

            return default;
        }
    }
}
