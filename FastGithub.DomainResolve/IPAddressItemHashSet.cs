using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    sealed class IPAddressItemHashSet
    {
        private readonly object syncRoot = new();

        private readonly HashSet<IPAddressItem> hashSet = new();

        public int Count => this.hashSet.Count;

        public bool Add(IPAddressItem item)
        {
            lock (this.syncRoot)
            {
                return this.hashSet.Add(item);
            }
        }

        public void AddRange(IEnumerable<IPAddressItem> items)
        {
            lock (this.syncRoot)
            {
                foreach (var item in items)
                {
                    this.hashSet.Add(item);
                }
            }
        }

        public IPAddressItem[] ToArray()
        {
            lock (this.syncRoot)
            {
                return this.hashSet.ToArray();
            }
        }

        public Task TestSpeedAsync()
        {
            var tasks = this.ToArray().Select(item => item.TestSpeedAsync());
            return Task.WhenAll(tasks);
        }
    }
}
