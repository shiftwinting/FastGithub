using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FastGithub.Scanner.DomainMiddlewares
{
    [Service(ServiceLifetime.Singleton, ServiceType = typeof(IDomainAddressProvider))]
    sealed class IPAddressComDomainAddressProvider : IDomainAddressProvider
    {
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly ILogger<IPAddressComDomainAddressProvider> logger;
        private readonly Uri lookupUri = new("https://www.ipaddress.com/ip-lookup");

        public IPAddressComDomainAddressProvider(
             IOptionsMonitor<GithubOptions> options,
            ILogger<IPAddressComDomainAddressProvider> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        public async Task<IEnumerable<DomainAddress>> CreateDomainAddressesAsync()
        {
            var setting = this.options.CurrentValue.DominAddressProvider.IPAddressComDomainAddress;
            if (setting.Enable == false)
            {
                return Enumerable.Empty<DomainAddress>();
            }

            using var httpClient = new HttpClient();
            var result = new List<DomainAddress>();
            foreach (var domain in setting.Domains)
            {
                try
                {
                    var addresses = await this.LookupAsync(httpClient, domain);
                    foreach (var address in addresses)
                    {
                        result.Add(new DomainAddress(domain, address));
                    }
                }
                catch (Exception)
                {
                    this.logger.LogWarning($"ipaddress.com无法解析{domain}");
                }
            }
            return result;
        }

        private async Task<List<IPAddress>> LookupAsync(HttpClient httpClient, string domain)
        {
            var keyValue = new KeyValuePair<string?, string?>("host", domain);
            var content = new FormUrlEncodedContent(Enumerable.Repeat(keyValue, 1));
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = lookupUri,
                Content = content
            };

            var response = await httpClient.SendAsync(request);
            var html = await response.Content.ReadAsStringAsync();
            var match = Regex.Match(html, @"(?<=<h1>IP Lookup : )\d+\.\d+\.\d+\.\d+", RegexOptions.IgnoreCase);

            if (match.Success && IPAddress.TryParse(match.Value, out var address))
            {
                return new List<IPAddress> { address };
            }

            var prefix = Regex.Escape("type=\"radio\" value=\"");
            var matches = Regex.Matches(html, @$"(?<={prefix})\d+\.\d+\.\d+\.\d+", RegexOptions.IgnoreCase);
            var addressList = new List<IPAddress>();
            foreach (Match item in matches)
            {
                if (IPAddress.TryParse(item.Value, out address))
                {
                    addressList.Add(address);
                }
            }
            return addressList;
        }
    }
}
