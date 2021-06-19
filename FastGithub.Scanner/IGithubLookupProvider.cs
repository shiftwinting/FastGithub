using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 定义域名的ip提值者
    /// </summary>
    interface IGithubLookupProvider
    {
        /// <summary>
        /// 获取排序
        /// </summary>
        int Order { get; }

        /// <summary>
        /// 查找域名与ip关系
        /// </summary>
        /// <param name="domains"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<DomainAddress>> LookupAsync(IEnumerable<string> domains, CancellationToken cancellationToken);
    }
}
