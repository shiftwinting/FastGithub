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
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IPAddress?> ResolveAsync(string domain, CancellationToken cancellationToken = default);

        /// <summary>
        /// 解析所有ip
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<IPAddress> ResolveAllAsync(string domain, CancellationToken cancellationToken = default);
    }
}