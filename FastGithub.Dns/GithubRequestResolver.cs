using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using FastGithub.Scanner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Net;
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
            ILogger<GithubRequestResolver> logger)
        {
            this.githubScanResults = githubScanResults;
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
        {
            var response = Response.FromRequest(request);
            var question = request.Questions.FirstOrDefault();

            if (question != null && question.Type == RecordType.A)
            {
                var domain = question.Name.ToString();
                var address = this.githubScanResults.FindBestAddress(domain);

                if (address != null)
                {
                    address = IPAddress.Loopback;
                    var ttl = this.options.CurrentValue.GithubTTL;
                    var record = new IPAddressResourceRecord(question.Name, address, ttl);
                    response.AnswerRecords.Add(record);
                    this.logger.LogInformation(record.ToString());
                }
            }

            return Task.FromResult<IResponse>(response);
        }
    }
}
