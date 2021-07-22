using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns.DnscryptProxy
{
    /// <summary>
    /// DnscryptProxy服务
    /// </summary>
    sealed class DnscryptProxyService
    {
        private readonly FastGithubConfig fastGithubConfig;

        /// <summary>
        /// 获取文件名
        /// </summary>
        public string Name => "dnscrypt-proxy";

        /// <summary>
        /// DnscryptProxy服务
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        public DnscryptProxyService(FastGithubConfig fastGithubConfig)
        {
            this.fastGithubConfig = fastGithubConfig;
        }

        /// <summary>
        /// 启动dnscrypt-proxy
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var tomlPath = $"{this.Name}.toml";
            await TomlUtil.SetListensAsync(tomlPath, this.fastGithubConfig.PureDns, cancellationToken);

            foreach (var process in Process.GetProcessesByName(this.Name))
            {
                process.Kill();
            }

            if (OperatingSystem.IsWindows())
            {
                this.StartDnscryptProxy("-service install", waitForExit: true);
                this.StartDnscryptProxy("-service start", waitForExit: true);
            }
            else
            {
                this.StartDnscryptProxy(string.Empty, waitForExit: false);
            }
        }

        /// <summary>
        /// 停止dnscrypt-proxy
        /// </summary>
        public void Stop()
        {
            if (OperatingSystem.IsWindows())
            {
                this.StartDnscryptProxy("-service stop", waitForExit: true);
                this.StartDnscryptProxy("-service uninstall", waitForExit: true);
            }

            foreach (var process in Process.GetProcessesByName(this.Name))
            {
                process.Kill();
            }
        }

        /// <summary>
        /// 等待退出
        /// </summary>
        public void WaitForExit()
        {
            var process = Process.GetProcessesByName(this.Name).FirstOrDefault();
            if (process != null)
            {
                process.WaitForExit();
            }
        }

        /// <summary>
        /// 启动DnscryptProxy进程
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="waitForExit"></param> 
        private void StartDnscryptProxy(string arguments, bool waitForExit)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? $"{this.Name}.exe" : this.Name,
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

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
