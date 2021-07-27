using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// 代理验证
    /// </summary>
    sealed class ProxyValidtor : IDnsValidator
    {
        private readonly IOptions<FastGithubOptions> options;
        private readonly ILogger<ProxyValidtor> logger;

        public ProxyValidtor(
            IOptions<FastGithubOptions> options,
            ILogger<ProxyValidtor> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// 验证是否使用了代理
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

            foreach (var domain in this.options.Value.DomainConfigs.Keys)
            {
                var destination = new Uri($"https://{domain.Replace('*', 'a')}");
                var proxyServer = systemProxy.GetProxy(destination);
                if (proxyServer != null)
                {
                    this.logger.LogError($"由于系统配置了{proxyServer}代理{domain}，{nameof(FastGithub)}无法加速相关域名");
                }
            }
        }
    }
}
