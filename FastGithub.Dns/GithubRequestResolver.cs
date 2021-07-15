using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using FastGithub.Scanner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
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
        private readonly IGithubResolver githubResolver;
        private readonly IOptionsMonitor<DnsOptions> options;
        private readonly ILogger<GithubRequestResolver> logger;

        /// <summary>
        /// github相关域名解析器
        /// </summary>
        /// <param name="githubResolver"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public GithubRequestResolver(
            IGithubResolver githubResolver,
            IOptionsMonitor<DnsOptions> options,
            ILogger<GithubRequestResolver> logger)
        {
            this.githubResolver = githubResolver;
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
            IResponse response = Response.FromRequest(request);
            var question = request.Questions.FirstOrDefault();

            if (question == null || question.Type != RecordType.A)
            {
                return Task.FromResult(response);
            }

            var domain = question.Name.ToString();
            if (this.githubResolver.IsSupported(domain) == false)
            {
                return Task.FromResult(response);
            }

            if (this.options.CurrentValue.UseGithubReverseProxy == false)
            {
                var address = this.githubResolver.Resolve(domain);
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
                var address = IPAddress.Parse(this.options.CurrentValue.GithubReverseProxyIPAddress);
                var record = new IPAddressResourceRecord(question.Name, address, TimeSpan.FromMinutes(1));
                response.AnswerRecords.Add(record);
                this.logger.LogInformation($"[{domain}->{address}]");
            } 
            return Task.FromResult(response);
        }
    }
}
