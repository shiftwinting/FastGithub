using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace FastGithub.Scanner
{
    sealed class GithubContextCollection
    {
        private readonly object syncRoot = new();
        private readonly HashSet<GithubContext> contextHashSet = new();
        private readonly Dictionary<string, IPAddress> domainAdressCache = new();

        public void AddOrUpdate(GithubContext context)
        {
            lock (this.syncRoot)
            {
                if (this.contextHashSet.TryGetValue(context, out var value))
                {
                    value.Elapsed = context.Elapsed;
                    value.Available = context.Available;
                }
                else
                {
                    this.contextHashSet.Add(context);
                }
            }
        }

        public GithubContext[] ToArray()
        {
            lock (this.syncRoot)
            {
                return this.contextHashSet.ToArray();
            }
        }

        /// <summary>
        /// 查找又稳又快的ip
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public IPAddress? FindFastAddress(string domain)
        {
            lock (this.syncRoot)
            {
                // 如果上一次的ip可以使用，就返回上一次的ip
                if (this.domainAdressCache.TryGetValue(domain, out var address))
                {
                    var key = new GithubContext(domain, address);
                    if (this.contextHashSet.TryGetValue(key, out var context) && context.Available)
                    {
                        return address;
                    }
                }

                var fastAddress = this.contextHashSet
                    .Where(item => item.Available && item.Domain == domain)
                    .OrderBy(item => item.Elapsed)
                    .Select(item => item.Address)
                    .FirstOrDefault();

                if (fastAddress != null)
                {
                    this.domainAdressCache[domain] = fastAddress;
                }
                else
                {
                    this.domainAdressCache.Remove(domain);
                }
                return fastAddress;
            }
        }
    }
}
