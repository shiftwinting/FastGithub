using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class DnsHostedService : IHostedService, IRequestResolver
    {
        private readonly DnsServer dnsServer;
        private readonly GithubScanService githubScanService;
        private readonly ILogger<DnsHostedService> logger;

        public DnsHostedService(
            GithubScanService githubScanService,
            IOptions<DnsOptions> options,
            ILogger<DnsHostedService> logger)
        {
            this.dnsServer = new DnsServer(this, options.Value.UpStream);
            this.githubScanService = githubScanService;
            this.logger = logger;
        }

        /// <summary>
        /// 解析dns
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IResponse> IRequestResolver.Resolve(IRequest request, CancellationToken cancellationToken)
        {
            var response = Response.FromRequest(request);
            var question = request.Questions.FirstOrDefault();

            if (question != null && question.Type == RecordType.A)
            {
                var domain = question.Name.ToString();
                if (domain.Contains("github", StringComparison.OrdinalIgnoreCase))
                {
                    var contexts = this.githubScanService.Result
                        .Where(item => item.Domain == domain)
                        .OrderBy(item => item.HttpElapsed);

                    foreach (var context in contexts)
                    {
                        var record = new IPAddressResourceRecord(question.Name, context.Address, TimeSpan.FromMinutes(2d));
                        response.AnswerRecords.Add(record);
                        this.logger.LogWarning(record.ToString());
                    }

                    return Task.FromResult<IResponse>(response);
                }
            }

            return Task.FromResult<IResponse>(response);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.dnsServer.Listen();
            this.logger.LogInformation("dns服务启用成功");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.dnsServer.Dispose();
            this.logger.LogInformation("dns服务已终止");
            return Task.CompletedTask;
        }
    }
}
