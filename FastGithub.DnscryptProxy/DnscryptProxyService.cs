using DNS.Client;
using DNS.Protocol;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DnscryptProxy
{
    /// <summary>
    /// DnscryptProxy服务
    /// </summary>
    sealed class DnscryptProxyService
    {
        private const string name = "dnscrypt-proxy";
        private const string testDomain = "api.github.com";
        private readonly FastGithubConfig fastGithubConfig;

        /// <summary>
        /// 获取相关进程
        /// </summary>
        public Process? Process { get; private set; }

        /// <summary>
        /// 获取服务控制状态
        /// </summary>
        public ControllState ControllState { get; private set; } = ControllState.None;

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
            this.ControllState = ControllState.Started;

            var tomlPath = $"{name}.toml";
            await TomlUtil.SetListensAsync(tomlPath, this.fastGithubConfig.PureDns, cancellationToken);

            foreach (var process in Process.GetProcessesByName(name))
            {
                process.Kill();
            }

            if (OperatingSystem.IsWindows())
            {
                StartDnscryptProxy("-service install")?.WaitForExit();
                StartDnscryptProxy("-service start")?.WaitForExit();
                this.Process = Process.GetProcessesByName(name).FirstOrDefault(item => item.SessionId == 0);
            }
            else
            {
                this.Process = StartDnscryptProxy(string.Empty);
            }
        }

        /// <summary>
        /// 等待dns服务OK
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task WaitForDnsOKAsync(CancellationToken cancellationToken)
        {
            var process = this.Process;
            if (process == null || process.HasExited || this.ControllState != ControllState.Started)
            {
                return;
            }

            using var processExitTokenSource = new CancellationTokenSource();
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => processExitTokenSource.Cancel();

            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, processExitTokenSource.Token);
            await this.WaitForDnsOKCoreAsync(linkedTokenSource.Token);
        }

        /// <summary>
        /// 等待dns服务OK
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task WaitForDnsOKCoreAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1d));
                    using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                    var dnsClient = new DnsClient(this.fastGithubConfig.PureDns);
                    await dnsClient.Lookup(testDomain, RecordType.A, linkedTokenSource.Token);
                    break;
                }
                catch (Exception)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        /// <summary>
        /// 停止dnscrypt-proxy
        /// </summary>
        public void Stop()
        {
            this.ControllState = ControllState.Stopped;

            if (OperatingSystem.IsWindows())
            {
                StartDnscryptProxy("-service stop")?.WaitForExit();
                StartDnscryptProxy("-service uninstall")?.WaitForExit();
            }

            if (this.Process != null && this.Process.HasExited == false)
            {
                this.Process.Kill();
            }
        }

        /// <summary>
        /// 启动DnscryptProxy进程
        /// </summary>
        /// <param name="arguments"></param> 
        private static Process? StartDnscryptProxy(string arguments)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? $"{name}.exe" : name,
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return name;
        }
    }
}
