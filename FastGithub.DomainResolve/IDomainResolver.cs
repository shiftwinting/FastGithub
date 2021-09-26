using System.Collections.Generic;
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
        /// 解析域名
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IPAddress> ResolveAsync(DnsEndPoint domain, CancellationToken cancellationToken = default);

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<IPAddress> ResolveAsync(string domain, CancellationToken cancellationToken = default);
    }
}