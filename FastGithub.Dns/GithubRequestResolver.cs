using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using FastGithub.ReverseProxy;
using FastGithub.Scanner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// github相关域名解析器
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class GithubRequestResolver : IRequestResolver
    {
        private readonly IGithubScanResults githubScanResults;
        private readonly IOptionsMonitor<DnsOptions> options;
        private readonly IOptionsMonitor<GithubReverseProxyOptions> reverseProxyOptions;
        private readonly ILogger<GithubRequestResolver> logger;

        /// <summary>
        /// github相关域名解析器
        /// </summary>
        /// <param name="githubScanResults"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public GithubRequestResolver(
            IGithubScanResults githubScanResults,
            IOptionsMonitor<DnsOptions> options,
            IOptionsMonitor<GithubReverseProxyOptions> reverseProxyOptions,
            ILogger<GithubRequestResolver> logger)
        {
            this.githubScanResults = githubScanResults;
            this.options = options;
            this.reverseProxyOptions = reverseProxyOptions;
            this.logger = logger;
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
        {
            var response = Response.FromRequest(request);
            var question = request.Questions.FirstOrDefault();

            if (question == null || question.Type != RecordType.A)
            {
                return response;
            }

            var domain = question.Name.ToString();
            if (this.githubScanResults.Support(domain) == false)
            {
                return response;
            }

            if (this.reverseProxyOptions.CurrentValue.Enable == false)
            {
                var address = this.githubScanResults.FindBestAddress(domain);
                if (address != null)
                {
                    var ttl = this.options.CurrentValue.GithubTTL;
                    var record = new IPAddressResourceRecord(question.Name, address, ttl);
                    response.AnswerRecords.Add(record);
                    this.logger.LogInformation($"[{domain}->{address}]");
                }
            }
            else
            {
                var address = await GetLocalHostAddress();
                var record = new IPAddressResourceRecord(question.Name, address, TimeSpan.FromMinutes(1));
                response.AnswerRecords.Add(record);
                this.logger.LogInformation($"[{domain}->{address}]");
            }

            if (response.AnswerRecords.Count == 0)
            {
                this.logger.LogWarning($"无法获得{domain}的最快ip");
            }

            return response;
        }

        /// <summary>
        /// 获取本机ip
        /// </summary>
        /// <returns></returns>
        private static async Task<IPAddress> GetLocalHostAddress()
        {
            try
            {
                var localhost = System.Net.Dns.GetHostName();
                var addresses = await System.Net.Dns.GetHostAddressesAsync(localhost);
                var address = addresses.FirstOrDefault(item => item.AddressFamily == AddressFamily.InterNetwork);
                return address ?? IPAddress.Loopback;
            }
            catch (Exception)
            {
                return IPAddress.Loopback;
            }
        }
    }
}
