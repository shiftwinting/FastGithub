using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace FastGithub.Scanner
{
    /// <summary>
    ///  GithubContext集合
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class GithubScanResults : IGithubScanResults
    {
        private readonly object syncRoot = new();
        private readonly List<GithubContext> contexts = new();
        private readonly IOptionsMonitor<GithubLookupFactoryOptions> options;

        public GithubScanResults(IOptionsMonitor<GithubLookupFactoryOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// 添加GithubContext
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool Add(GithubContext context)
        {
            lock (this.syncRoot)
            {
                if (this.contexts.Contains(context))
                {
                    return false;
                }
                this.contexts.Add(context);
                return true;
            }
        }

        /// <summary>
        /// 转换为数组
        /// </summary>
        /// <returns></returns>
        public GithubContext[] ToArray()
        {
            lock (this.syncRoot)
            {
                return this.contexts.ToArray();
            }
        }

        /// <summary>
        /// 是否支持指定域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public bool Support(string domain)
        {
            return this.options.CurrentValue.Domains.Contains(domain);
        }

        /// <summary>
        /// 查找最优的ip
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public IPAddress? FindBestAddress(string domain)
        {
            if (this.Support(domain) == false)
            {
                return default;
            }

            lock (this.syncRoot)
            {
                return this.contexts
                    .Where(item => item.Domain == domain && item.AvailableRate > 0d)
                    .OrderByDescending(item => item.AvailableRate)
                    .ThenByDescending(item => item.Available)
                    .ThenBy(item => item.AvgElapsed)
                    .Select(item => item.Address)
                    .FirstOrDefault();
            }
        }
    }
}
