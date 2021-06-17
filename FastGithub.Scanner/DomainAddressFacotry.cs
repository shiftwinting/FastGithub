using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    [Service(ServiceLifetime.Singleton)]
    sealed class DomainAddressFacotry
    {
        private readonly IEnumerable<IDomainAddressProvider> providers;

        public DomainAddressFacotry(IEnumerable<IDomainAddressProvider> providers)
        {
            this.providers = providers;
        }

        public async Task<IEnumerable<DomainAddress>> CreateDomainAddressesAsync()
        {
            var hashSet = new HashSet<DomainAddress>();
            foreach (var provider in this.providers)
            {
                var domainAddresses = await provider.CreateDomainAddressesAsync();
                foreach (var item in domainAddresses)
                {
                    hashSet.Add(item);
                }
            }
            return hashSet;
        }
    }
}
