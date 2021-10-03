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
        /// 解析ip
        /// </summary>
        /// <param name="endPoint">节点</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IPAddress> ResolveAnyAsync(DnsEndPoint endPoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// 解析所有ip
        /// </summary>
        /// <param name="endPoint">节点</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<IPAddress> ResolveAllAsync(DnsEndPoint endPoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// 对所有节点进行测速
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task TestAllEndPointsAsync(CancellationToken cancellationToken = default);
    }
}