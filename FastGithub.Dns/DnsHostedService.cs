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
    sealed class DnsHostedService : BackgroundService
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
            this.dnsServer.Listening += DnsServer_Listening;
            this.dnsServer.Errored += DnsServer_Errored;
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// 监听后
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DnsServer_Listening(object? sender, EventArgs e)
        {
            this.logger.LogInformation("dns服务启动成功");
            this.dnsAddresses = this.SetNameServers(IPAddress.Loopback, this.options.Value.UpStream);
        }

        /// <summary>
        /// dns异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DnsServer_Errored(object? sender, DnsServer.ErroredEventArgs e)
        {
            if (e.Exception is not OperationCanceledException)
            {
                this.logger.LogError($"dns服务异常：{e.Exception.Message}");
            }
        }

        /// <summary>
        /// 启动dns
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await this.dnsServer.Listen();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"dns服务启动失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 停止dns服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
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
