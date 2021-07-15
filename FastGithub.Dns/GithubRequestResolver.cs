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
            if (request is not RemoteRequest remoteRequest)
            {
                return Task.FromResult(response);
            }

            var question = request.Questions.FirstOrDefault();
            if (question == null || question.Type != RecordType.A)
            {
                return Task.FromResult(response);
            }

            var domain = question.Name;
            if (this.githubResolver.IsSupported(domain.ToString()) == false)
            {
                return Task.FromResult(response);
            }

            var record = this.GetAnswerRecord(remoteRequest, domain);
            if (record != null)
            {
                this.logger.LogInformation($"[{domain}->{record.IPAddress}]");
                response.AnswerRecords.Add(record);
            }

            return Task.FromResult(response);
        }

        /// <summary>
        /// 获取答案
        /// </summary>
        /// <param name="request"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        private IPAddressResourceRecord? GetAnswerRecord(RemoteRequest request, Domain domain)
        {
            if (this.options.CurrentValue.UseGithubReverseProxy == true)
            {
                var localAddress = request.GetLocalAddress() ?? IPAddress.Loopback;
                return new IPAddressResourceRecord(domain, localAddress, TimeSpan.FromMinutes(1d));
            }

            var githubAddress = this.githubResolver.Resolve(domain.ToString());
            if (githubAddress == null)
            {
                return default;
            }

            var ttl = this.options.CurrentValue.GithubTTL;
            return new IPAddressResourceRecord(domain, githubAddress, ttl);
        }
    }
}
