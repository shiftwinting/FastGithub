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
        /// <param name="endPoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IPAddress> ResolveAsync(DnsEndPoint endPoint, CancellationToken cancellationToken = default);
    }
}