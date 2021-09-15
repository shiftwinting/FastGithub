using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
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
        public async Task ValidateAsync()
        {
            var systemProxy = HttpClient.DefaultProxy;
            if (systemProxy == null)
            {
                return;
            }

            var httpProxyPort = this.options.Value.HttpProxyPort;
            foreach (var domain in this.options.Value.DomainConfigs.Keys)
            {
                var destination = new Uri($"https://{domain.Replace('*', 'a')}");
                var proxyServer = systemProxy.GetProxy(destination);

                if (proxyServer == null)
                {
                    continue;
                }

                if (await IsFastGithubProxyAsync(proxyServer, httpProxyPort) == false)
                {
                    this.logger.LogError($"由于系统配置了{proxyServer}代理{domain}，{nameof(FastGithub)}无法加速相关域名");
                }
            }
        }

        /// <summary>
        /// 是否为fastgithub代理
        /// </summary>
        /// <param name="proxyServer"></param>
        /// <param name="httpProxyPort"></param>
        /// <returns></returns>
        private static async Task<bool> IsFastGithubProxyAsync(Uri proxyServer, int httpProxyPort)
        {
            if (proxyServer.Port != httpProxyPort)
            {
                return false;
            }

            if (IPAddress.TryParse(proxyServer.Host, out var address))
            {
                return address.Equals(IPAddress.Loopback);
            }

            try
            {
                var addresses = await System.Net.Dns.GetHostAddressesAsync(proxyServer.Host);
                return addresses.Contains(IPAddress.Loopback);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
