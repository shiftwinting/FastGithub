using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
    sealed class GithubLookupFacotry
    {
        private readonly IEnumerable<IGithubLookupProvider> providers;
        private readonly IOptionsMonitor<GithubLookupFactoryOptions> options;

        /// <summary>
        /// 域名与ip关系工厂
        /// </summary>
        /// <param name="providers"></param>
        /// <param name="options"></param>
        public GithubLookupFacotry(
            IEnumerable<IGithubLookupProvider> providers,
            IOptionsMonitor<GithubLookupFactoryOptions> options)
        {
            this.providers = providers.OrderBy(item => item.Order);
            this.options = options;
        }

        /// <summary>
        /// 查找域名与ip关系
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<DomainAddress>> LookupAsync(CancellationToken cancellationToken)
        {
            var hashSet = new HashSet<DomainAddress>();
            var domains = this.options.CurrentValue.Domains;

            foreach (var provider in this.providers)
            {
                var domainAddresses = await provider.LookupAsync(domains, cancellationToken);
                foreach (var item in domainAddresses)
                {
                    hashSet.Add(item);
                }
            }
            return hashSet;
        }
    }
}
