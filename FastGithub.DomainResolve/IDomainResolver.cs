using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名解析器
    /// </summary>
    public interface IDomainResolver
    {
        /// <summary>
        /// 设置ip黑名单
        /// </summary>
        /// <param name="address">ip</param>
        /// <param name="expiration">过期时间</param>
        void SetBlack(IPAddress address, TimeSpan expiration);

        /// <summary>
        /// 刷新域名解析结果
        /// </summary>
        /// <param name="domain">域名</param>
        void FlushDomain(DnsEndPoint domain);

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IPAddress> ResolveAsync(DnsEndPoint domain, CancellationToken cancellationToken = default);
    }
}