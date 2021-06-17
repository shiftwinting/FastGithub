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
    /// <summary>
    /// ipaddress.com的域名与ip关系提供者
    /// </summary>
    [Service(ServiceLifetime.Singleton, ServiceType = typeof(IDomainAddressProvider))]
    sealed class IPAddressComProvider : IDomainAddressProvider
    {
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly ILogger<IPAddressComProvider> logger;
        private readonly Uri lookupUri = new("https://www.ipaddress.com/ip-lookup");

        /// <summary>
        /// ipaddress.com的域名与ip关系提供者
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public IPAddressComProvider(
            IOptionsMonitor<GithubOptions> options,
            ILogger<IPAddressComProvider> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// 创建域名与ip的关系
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<DomainAddress>> CreateDomainAddressesAsync()
        {
            var setting = this.options.CurrentValue.DominAddressProviders.IPAddressComProvider;
            if (setting.Enable == false)
            {
                return Enumerable.Empty<DomainAddress>();
            }

            using var httpClient = new HttpClient();
            var result = new HashSet<DomainAddress>();
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

        /// <summary>
        /// 反查ip
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        private async Task<List<IPAddress>> LookupAsync(HttpClient httpClient, string domain)
        {
            var keyValue = new KeyValuePair<string?, string?>("host", domain);
            var content = new FormUrlEncodedContent(Enumerable.Repeat(keyValue, 1));
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = lookupUri,
                Content = content
            };

            using var response = await httpClient.SendAsync(request);
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
