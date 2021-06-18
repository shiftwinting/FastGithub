using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 域名与ip关系工厂
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class DomainAddressFacotry
    {
        private readonly IEnumerable<IDomainAddressProvider> providers;

        /// <summary>
        /// 域名与ip关系工厂
        /// </summary>
        /// <param name="providers"></param>
        public DomainAddressFacotry(IEnumerable<IDomainAddressProvider> providers)
        {
            this.providers = providers.OrderBy(item => item.Order);
        }

        /// <summary>
        /// 创建域名与ip的关系
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<DomainAddress>> CreateDomainAddressesAsync(CancellationToken cancellationToken)
        {
            var hashSet = new HashSet<DomainAddress>();
            foreach (var provider in this.providers)
            {
                var domainAddresses = await provider.CreateDomainAddressesAsync(cancellationToken);
                foreach (var item in domainAddresses)
                {
                    hashSet.Add(item);
                }
            }
            return hashSet;
        }
    }
}
