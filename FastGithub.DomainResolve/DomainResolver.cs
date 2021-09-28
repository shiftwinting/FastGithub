using FastGithub.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名解析器
    /// </summary> 
    sealed class DomainResolver : IDomainResolver
    {
        private readonly DomainSpeedTester speedTester;

        /// <summary>
        /// 域名解析器
        /// </summary> 
        /// <param name="speedTester"></param>
        public DomainResolver(DomainSpeedTester speedTester)
        {
            this.speedTester = speedTester;
        }

        /// <summary>
        /// 解析ip
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddress> ResolveAsync(string domain, CancellationToken cancellationToken = default)
        {
            await foreach (var address in this.ResolveAllAsync(domain, cancellationToken))
            {
                return address;
            }
            throw new FastGithubException($"解析不到{domain}的IP");
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<IPAddress> ResolveAllAsync(string domain, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (this.speedTester.TryGetOrderAllIPAddresses(domain, out var addresses))
            {
                foreach (var address in addresses)
                {
                    yield return address;
                }
            }
            else
            {
                this.speedTester.Add(domain);
                await foreach (var address in this.speedTester.GetOrderAnyIPAddressAsync(domain, cancellationToken))
                {
                    yield return address;
                }
            }
        }
    }
}
