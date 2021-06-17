using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    interface IDomainAddressProvider
    {
        Task<IEnumerable<DomainAddress>> CreateDomainAddressesAsync();
    }
}
