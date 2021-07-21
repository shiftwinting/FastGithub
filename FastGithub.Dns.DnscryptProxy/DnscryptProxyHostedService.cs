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
        private const string dnscryptProxyFile = "dnscrypt-proxy";
        private readonly ILogger<DnscryptProxyHostedService> logger;

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
                if (OperatingSystem.IsWindows())
                {
                    StartDnscryptProxy("-service install", waitForExit: true);
                    StartDnscryptProxy("-service start", waitForExit: true);
                }
                else
                {
                    StartDnscryptProxy(string.Empty, waitForExit: false);
                }
                this.logger.LogInformation($"{dnscryptProxyFile}启动成功");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{dnscryptProxyFile}启动失败：{ex.Message}");
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
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    StartDnscryptProxy("-service stop", waitForExit: true);
                    StartDnscryptProxy("-service uninstall", waitForExit: true);
                }

                foreach (var process in Process.GetProcessesByName(dnscryptProxyFile))
                {
                    process.Kill();
                }
                this.logger.LogInformation($"{dnscryptProxyFile}已停止");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{dnscryptProxyFile}停止失败：{ex.Message}");
            }
            return Task.CompletedTask;
        }


        /// <summary>
        /// 启动DnscryptProxy进程
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="waitForExit"></param> 
        private static void StartDnscryptProxy(string arguments, bool waitForExit)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? $"{dnscryptProxyFile}.exe" : dnscryptProxyFile,
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            if (waitForExit && process != null)
            {
                process.WaitForExit();
            }
        }
    }
}
