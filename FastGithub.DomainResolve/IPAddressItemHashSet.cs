using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// IPAddressItem集合
    /// </summary>
    sealed class IPAddressItemHashSet
    {
        private readonly object syncRoot = new();
        private readonly HashSet<IPAddressItem> hashSet = new();

        /// <summary>
        /// 获取元素数量
        /// </summary>
        public int Count => this.hashSet.Count;

        /// <summary>
        /// 添加元素
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Add(IPAddressItem item)
        {
            lock (this.syncRoot)
            {
                return this.hashSet.Add(item);
            }
        }

        /// <summary>
        /// 转换为数组
        /// </summary>
        /// <returns></returns>
        public IPAddressItem[] ToArray()
        {
            lock (this.syncRoot)
            {
                return this.hashSet.ToArray();
            }
        }

        /// <summary>
        /// Ping所有IP
        /// </summary>
        /// <returns></returns>
        public Task PingAllAsync()
        {
            var items = this.ToArray();
            if (items.Length == 0)
            {
                return Task.CompletedTask;
            }
            if (items.Length == 1)
            {
                return items[0].PingAsync();
            }
            var tasks = items.Select(item => item.PingAsync());
            return Task.WhenAll(tasks);
        }
    }
}
