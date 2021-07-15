using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;

namespace FastGithub.Scanner
{
    /// <summary>
    /// github解析器
    /// </summary>
    [Service(ServiceLifetime.Singleton, ServiceType = typeof(IGithubResolver))]
    sealed class GithubResolver : IGithubResolver
    {
        private readonly GithubScanResults githubScanResults;
        private readonly IOptionsMonitor<GithubLookupFactoryOptions> options;

        /// <summary>
        /// github解析器
        /// </summary>
        /// <param name="githubScanResults"></param>
        /// <param name="options"></param>
        public GithubResolver(
            GithubScanResults githubScanResults,
            IOptionsMonitor<GithubLookupFactoryOptions> options)
        {
            this.githubScanResults = githubScanResults;
            this.options = options;
        }

        /// <summary>
        /// 是否支持指定的域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public bool IsSupported(string domain)
        {
            return this.options.CurrentValue.Domains.Contains(domain);
        }

        /// <summary>
        /// 解析指定的域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public IPAddress? Resolve(string domain)
        {
            return this.IsSupported(domain) ? this.githubScanResults.FindBestAddress(domain) : default;
        }
    }
}
