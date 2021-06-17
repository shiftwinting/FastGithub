using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace FastGithub.Scanner
{
    [Service(ServiceLifetime.Singleton)]
    sealed class GithubContextCollection : IGithubScanResults
    {
        private readonly object syncRoot = new();
        private readonly List<GithubContext> contextList = new();

        public bool Add(GithubContext context)
        {
            lock (this.syncRoot)
            {
                if (this.contextList.Contains(context))
                {
                    return false;
                }
                this.contextList.Add(context);
                return true;
            }
        }


        public GithubContext[] ToArray()
        {
            lock (this.syncRoot)
            {
                return this.contextList.ToArray();
            }
        }


        public bool IsAvailable(string domain, IPAddress address)
        {
            lock (this.syncRoot)
            {
                var target = new GithubContext(domain, address);
                var context = this.contextList.Find(item => item.Equals(target));
                return context != null && context.Available;
            }
        }

        /// <summary>
        /// 查找又稳又快的ip
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public IPAddress? FindBestAddress(string domain)
        {
            lock (this.syncRoot)
            {
                return this.contextList
                    .Where(item => item.Domain == domain && item.History.AvailableRate > 0d)
                    .OrderByDescending(item => item.History.AvailableRate)
                    .ThenBy(item => item.History.AvgElapsed)
                    .Select(item => item.Address)
                    .FirstOrDefault();
            }
        }
    }
}
