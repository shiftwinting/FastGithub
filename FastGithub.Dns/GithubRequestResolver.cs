using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using FastGithub.Scanner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    [Service(ServiceLifetime.Singleton)]
    sealed class GithubRequestResolver : IRequestResolver
    {
        private readonly IGithubScanService githubScanService;
        private readonly ILogger<GithubRequestResolver> logger;

        public GithubRequestResolver(
            IGithubScanService githubScanService,
            ILogger<GithubRequestResolver> logger)
        {
            this.githubScanService = githubScanService;
            this.logger = logger;
        }

        public Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
        {
            var response = Response.FromRequest(request);
            var question = request.Questions.FirstOrDefault();

            if (question != null && question.Type == RecordType.A)
            {
                var domain = question.Name.ToString();
                var fastAddress = this.githubScanService.FindFastAddress(domain);

                if (fastAddress != null)
                {
                    var record = new IPAddressResourceRecord(question.Name, fastAddress);
                    response.AnswerRecords.Add(record);
                    this.logger.LogInformation(record.ToString());
                }
            }

            return Task.FromResult<IResponse>(response);
        }
    }
}
