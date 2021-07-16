using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns.DnscryptProxy
{
    /// <summary>
    /// DnscryptProxy后台服务
    /// </summary>
    sealed class DnscryptProxyHostedService : IHostedService
    {
        private const string dnscryptFile = "dnscrypt-proxy";
        private readonly ILogger<DnscryptProxyHostedService> logger;
        private Process? dnscryptProcess;

        /// <summary>
        /// DnscryptProxy后台服务
        /// </summary>
        /// <param name="logger"></param>
        public DnscryptProxyHostedService(ILogger<DnscryptProxyHostedService> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 启动dnscrypt-proxy
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var fileName = dnscryptFile;
                if (OperatingSystem.IsWindows())
                {
                    fileName = $"{dnscryptFile}.exe";
                }

                if (File.Exists(fileName) == true)
                {
                    this.dnscryptProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = fileName,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    this.logger.LogInformation($"{dnscryptFile}启动成功");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{dnscryptFile}启动失败：{ex.Message}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止dnscrypt-proxy
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (this.dnscryptProcess != null)
            {
                this.dnscryptProcess.Kill();
            }
            return Task.CompletedTask;
        }
    }
}
