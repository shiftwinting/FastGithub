using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
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
                if (OperatingSystem.IsWindows() && Process.GetCurrentProcess().SessionId == 0)
                {
                    StartDnscrypt("-service install")?.WaitForExit();
                    StartDnscrypt("-service start")?.WaitForExit();
                }
                else
                {
                    this.dnscryptProcess = StartDnscrypt(string.Empty);
                }
                this.logger.LogInformation($"{dnscryptFile}启动成功");
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
                this.logger.LogInformation($"{dnscryptFile}已停止");
            }
            else if (OperatingSystem.IsWindows())
            {
                StartDnscrypt("-service stop")?.WaitForExit();
                StartDnscrypt("-service uninstall")?.WaitForExit();
                this.logger.LogInformation($"{dnscryptFile}已停止");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 启动Dnscrypt
        /// </summary>
        /// <param name="arguments"></param>
        private static Process? StartDnscrypt(string arguments)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? $"{dnscryptFile}.exe" : dnscryptFile,
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
    }
}
