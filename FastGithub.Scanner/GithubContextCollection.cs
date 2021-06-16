using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace FastGithub.Scanner
{
    sealed class GithubContextCollection
    {
        private readonly object syncRoot = new();
        private readonly HashSet<GithubContext> contextHashSet = new();

        public bool Add(GithubContext context)
        {
            lock (this.syncRoot)
            {
                return this.contextHashSet.Add(context);
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
        public IPAddress? FindBestAddress(string domain)
        {
            lock (this.syncRoot)
            {
                return this.contextHashSet
                    .Where(item => item.Available && item.Domain == domain)
                    .OrderByDescending(item => item.Statistics.GetSuccessRate())
                    .ThenBy(item => item.Statistics.GetAvgElapsed())
                    .Select(item => item.Address)
                    .FirstOrDefault();
            }
        }
    }
}
