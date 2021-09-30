using System;
using System.Diagnostics;
using System.Net;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// IP连接耗时
    /// </summary>
    [DebuggerDisplay("Adddress={Adddress} Elapsed={Elapsed}")]
    struct IPAddressElapsed
    {
        /// <summary>
        /// 获取IP地址
        /// </summary>
        public IPAddress Adddress { get; }

        /// <summary>
        /// 获取连接耗时
        /// </summary>
        public TimeSpan Elapsed { get; }

        /// <summary>
        /// IP连接耗时
        /// </summary>
        /// <param name="adddress"></param>
        /// <param name="elapsed"></param>
        public IPAddressElapsed(IPAddress adddress, TimeSpan elapsed)
        {
            this.Adddress = adddress;
            this.Elapsed = elapsed;
        }
    }
}
