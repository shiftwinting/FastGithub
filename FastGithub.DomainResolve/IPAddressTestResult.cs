using System;
using System.Collections.Generic;
using System.Linq;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// IP测速结果
    /// </summary>
    sealed class IPAddressTestResult
    {
        private static readonly TimeSpan lifeTime = TimeSpan.FromMinutes(2d);
        private readonly int creationTickCount = Environment.TickCount;

        /// <summary>
        /// 获取空的
        /// </summary>
        public static IPAddressTestResult Empty = new(Array.Empty<IPAddressElapsed>());

        /// <summary>
        /// 获取是否为空
        /// </summary>
        public bool IsEmpty => this.AddressElapseds.Length == 0;

        /// <summary>
        /// 获取是否已过期
        /// </summary>
        public bool IsExpired => lifeTime < TimeSpan.FromMilliseconds(Environment.TickCount - this.creationTickCount);

        /// <summary>
        /// 获取测速结果
        /// </summary>
        public IPAddressElapsed[] AddressElapseds { get; }

        /// <summary>
        /// 测速结果
        /// </summary>
        /// <param name="result"></param>
        public IPAddressTestResult(IEnumerable<IPAddressElapsed> addressElapseds)
        {
            this.AddressElapseds = addressElapseds.OrderBy(item => item.Elapsed).ToArray();
        }
    }
}
