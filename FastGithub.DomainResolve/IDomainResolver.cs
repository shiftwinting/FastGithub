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
        /// 解析可用的ip
        /// </summary>
        /// <param name="endPoint">远程节点</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IPAddress> ResolveAsync(DnsEndPoint endPoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// 解析所有ip
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<IPAddress> ResolveAsync(string domain, CancellationToken cancellationToken = default);
    }
}