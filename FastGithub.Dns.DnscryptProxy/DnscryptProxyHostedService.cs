using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns.DnscryptProxy
{
    /// <summary>
    /// DnscryptProxy后台服务
    /// </summary>
    sealed class DnscryptProxyHostedService : BackgroundService
    {
        private bool isStopped = false;
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
        public override Task StartAsync(CancellationToken cancellationToken)
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

            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// 后台监控
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            var process = Process.GetProcessesByName(dnscryptProxyFile).FirstOrDefault();
            if (process != null)
            {
                process.WaitForExit();
            }
            if (this.isStopped == false)
            {
                this.logger.LogCritical($"{dnscryptProxyFile}已停止运行，{nameof(FastGithub)}将无法解析域名。你可以把配置文件的{nameof(FastGithubOptions.PureDns)}修改为其它可用的DNS以临时使用。");
            }
        }

        /// <summary>
        /// 停止dnscrypt-proxy
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.isStopped = true;
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
            return base.StopAsync(cancellationToken);
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
