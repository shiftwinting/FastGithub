using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// 代理冲突验证
    /// </summary>
    sealed class ProxyConflictValidtor : IConflictValidator
    {
        private readonly IOptions<FastGithubOptions> options;
        private readonly ILogger<ProxyConflictValidtor> logger;

        public ProxyConflictValidtor(
            IOptions<FastGithubOptions> options,
            ILogger<ProxyConflictValidtor> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// 验证冲突
        /// </summary>
        /// <returns></returns>
        public Task ValidateAsync()
        {
            try
            {
                this.ValidateSystemProxy();
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 验证代理
        /// </summary>
        private void ValidateSystemProxy()
        {
            var systemProxy = HttpClient.DefaultProxy;
            if (systemProxy == null)
            {
                return;
            }

            var httpProxyPort = this.options.Value.HttpProxyPort;
            var loopbackProxyUri = new Uri($"http://127.0.0.1:{httpProxyPort}");
            var localhostProxyUri = new Uri($"http://localhost:{httpProxyPort}");

            foreach (var domain in this.options.Value.DomainConfigs.Keys)
            {
                var destination = new Uri($"https://{domain.Replace('*', 'a')}");
                var proxyServer = systemProxy.GetProxy(destination);
                if (proxyServer != null && proxyServer != loopbackProxyUri && proxyServer != localhostProxyUri)
                {
                    this.logger.LogError($"由于系统配置了{proxyServer}代理{domain}，{nameof(FastGithub)}无法加速相关域名");
                }
            }
        }
    }
}
