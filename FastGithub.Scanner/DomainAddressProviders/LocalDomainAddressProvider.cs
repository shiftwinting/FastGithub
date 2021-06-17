using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FastGithub.Scanner.DomainMiddlewares
{
    [Service(ServiceLifetime.Singleton, ServiceType = typeof(IDomainAddressProvider))]
    sealed class LocalDomainAddressProvider : IDomainAddressProvider
    {
        private const string JsonFile = "IPRange.json";
        private readonly ILogger<LocalDomainAddressProvider> logger;

        public LocalDomainAddressProvider(ILogger<LocalDomainAddressProvider> logger)
        {
            this.logger = logger;
        }

        public async Task<IEnumerable<DomainAddress>> CreateDomainAddressesAsync()
        {
            try
            {
                if (File.Exists(JsonFile) == true)
                {
                    using var fileStream = File.OpenRead(JsonFile);
                    var datas = await JsonSerializer.DeserializeAsync<Dictionary<string, string[]>>(fileStream);
                    if (datas != null)
                    {
                        return this.GetDomainAddresses(datas);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"加载本机的ip列表异常：{ex.Message}");
            }

            return Enumerable.Empty<DomainAddress>();
        }

        private IEnumerable<DomainAddress> GetDomainAddresses(Dictionary<string, string[]> datas)
        {
            foreach (var kv in datas)
            {
                var domain = kv.Key;
                foreach (var item in kv.Value)
                {
                    if (IPRange.TryParse(item, out var range))
                    {
                        foreach (var address in range)
                        {
                            yield return new DomainAddress(domain, address);
                        }
                    }
                }
            }
        }
    }
}
