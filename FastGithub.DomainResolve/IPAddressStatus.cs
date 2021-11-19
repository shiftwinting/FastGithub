using System;
using System.Net;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 表示IP的状态
    /// </summary>
    struct IPAddressStatus
    {
        /// <summary>
        /// 获取IP地址
        /// </summary>
        public IPAddress Address { get; }

        /// <summary>
        /// 获取延时
        /// 当连接失败时值为MaxValue
        /// </summary>
        public TimeSpan Elapsed { get; }


        /// <summary>
        /// IP的状态
        /// </summary>
        /// <param name="address"></param>
        /// <param name="elapsed"></param>
        public IPAddressStatus(IPAddress address, TimeSpan elapsed)
        {
            this.Address = address;
            this.Elapsed = elapsed;
        }
    }
}
