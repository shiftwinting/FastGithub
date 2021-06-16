using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;

namespace FastGithub.Scanner
{
    sealed class GithubContextCollection
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


        public bool TryGet(string domain, IPAddress address, [MaybeNullWhen(false)] out GithubContext context)
        {
            lock (this.syncRoot)
            {
                var target = new GithubContext(domain, address);
                context = this.contextList.Find(item => item.Equals(target));
                return context != null;
            }
        }

        public GithubContext[] ToArray()
        {
            lock (this.syncRoot)
            {
                return this.contextList.ToArray();
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
                    .Where(item => item.Available && item.Domain == domain)
                    .OrderByDescending(item => item.Statistics.GetSuccessRate())
                    .ThenBy(item => item.Statistics.GetAvgElapsed())
                    .Select(item => item.Address)
                    .FirstOrDefault();
            }
        }
    }
}
