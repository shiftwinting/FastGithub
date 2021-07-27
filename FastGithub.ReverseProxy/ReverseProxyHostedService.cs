using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 反向代理端口检测后台服务
    /// </summary>
    sealed class ReverseProxyHostedService : IHostedService
    {
        private readonly ILogger<ReverseProxyHostedService> logger;

        /// <summary>
        /// 反向代理端口检测后台服务
        /// </summary>
        /// <param name="logger"></param>
        public ReverseProxyHostedService(ILogger<ReverseProxyHostedService> logger)
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
            const int HTTPSPORT = 443;
            if (OperatingSystem.IsWindows())
            {
                if (TcpTable.TryGetOwnerProcessId(HTTPSPORT, out var pid))
                {
                    try
                    {
                        Process.GetProcessById(pid).Kill();
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (Exception)
                    {
                        var processName = Process.GetProcessById(pid).ProcessName;
                        this.logger.LogError($"由于进程{processName}({pid})占用了{HTTPSPORT}端口，{nameof(FastGithub)}的反向代理无法工作");
                    }
                }
            }
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
