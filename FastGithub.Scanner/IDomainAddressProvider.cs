using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 定义域名的ip提值者
    /// </summary>
    interface IDomainAddressProvider
    {
        /// <summary>
        /// 创建域名与ip的关系
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<DomainAddress>> CreateDomainAddressesAsync();
    }
}
