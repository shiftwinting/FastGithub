using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// IP延时集合
    /// </summary>
    [DebuggerDisplay("Count={Count} IsExpired={IsExpired}")]
    sealed class IPAddressElapsedCollection : IEnumerable<IPAddressElapsed>
    {
        private readonly List<IPAddressElapsed> addressElapseds;
        private readonly int creationTickCount = Environment.TickCount;
        private static readonly int maxLifeTime = 60 * 1000;

        /// <summary>
        /// 获取空的
        /// </summary>
        public static IPAddressElapsedCollection Empty = new();

        /// <summary>
        /// 获取数量
        /// </summary>
        public int Count => this.addressElapseds.Count;

        /// <summary>
        /// 获取是否为空
        /// </summary>
        public bool IsEmpty => this.addressElapseds.Count == 0;

        /// <summary>
        /// 获取是否已过期
        /// </summary>
        public bool IsExpired => Environment.TickCount - this.creationTickCount > maxLifeTime;

        /// <summary>
        /// IP延时集合
        /// </summary>
        private IPAddressElapsedCollection()
        {
            this.addressElapseds = new List<IPAddressElapsed>();
            this.creationTickCount = 0;
        }

        /// <summary>
        /// IP延时集合
        /// </summary>
        /// <param name="addressElapsed"></param>
        public IPAddressElapsedCollection(IPAddressElapsed addressElapsed)
        {
            this.addressElapseds = new List<IPAddressElapsed> { addressElapsed };
        }

        /// <summary>
        /// IP延时集合
        /// </summary>
        /// <param name="addressElapseds"></param>
        public IPAddressElapsedCollection(IEnumerable<IPAddressElapsed> addressElapseds)
        {
            this.addressElapseds = addressElapseds.OrderBy(item => item.Elapsed).ToList();
        }

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IPAddressElapsed> GetEnumerator()
        {
            return this.addressElapseds.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.addressElapseds.GetEnumerator();
        }
    }
}
