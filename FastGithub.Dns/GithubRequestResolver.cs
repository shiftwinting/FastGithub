using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using FastGithub.Scanner;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    [Service(ServiceLifetime.Singleton)]
    sealed class GithubRequestResolver : IRequestResolver
    {
        private readonly IGithubScanService githubScanService;
        private readonly IMemoryCache memoryCache;
        private readonly ILogger<GithubRequestResolver> logger;
        private readonly TimeSpan TTL = TimeSpan.FromMinutes(10d);

        public GithubRequestResolver(
            IGithubScanService githubScanService,
            IMemoryCache memoryCache,
            ILogger<GithubRequestResolver> logger)
        {
            this.githubScanService = githubScanService;
            this.memoryCache = memoryCache;
            this.logger = logger;
        }

        public Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
        {
            var response = Response.FromRequest(request);
            var question = request.Questions.FirstOrDefault();

            if (question != null && question.Type == RecordType.A)
            {
                var domain = question.Name.ToString();
                var address = this.GetGithubAddress(domain, TTL);

                if (address != null)
                {
                    var record = new IPAddressResourceRecord(question.Name, address);
                    response.AnswerRecords.Add(record);
                    this.logger.LogInformation(record.ToString());
                }
            }

            return Task.FromResult<IResponse>(response);
        }

        /// <summary>
        /// 模拟TTL
        /// 如果ip可用，则10分钟内返回缓存的ip，防止客户端ip频繁切换
        /// </summary> 
        /// <param name="domain"></param>
        /// <param name="ttl"></param>
        /// <returns></returns>
        private IPAddress? GetGithubAddress(string domain, TimeSpan ttl)
        {
            if (domain.Contains("github", StringComparison.OrdinalIgnoreCase) == false)
            {
                return default;
            }

            var key = $"ttl:{domain}";
            if (this.memoryCache.TryGetValue<IPAddress>(key, out var address))
            {
                if (this.githubScanService.IsAvailable(domain, address))
                {
                    return address;
                }
                this.memoryCache.Remove(key);
            }

            address = this.githubScanService.FindBestAddress(domain);
            if (address != null)
            {
                this.memoryCache.Set(key, address, ttl);
            }
            return address;
        }
    }
}
