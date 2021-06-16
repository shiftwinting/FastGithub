using System.Collections.Generic;
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
