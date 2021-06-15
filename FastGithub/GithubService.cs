using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class GithubService
    {
        private readonly GithubMetaService githubMetaService;
        private readonly GithubDelegate githubDelegate;
        private readonly ILogger<GithubService> logger;

        public GithubService(
            GithubMetaService githubMetaService,
            GithubDelegate githubDelegate,
            ILogger<GithubService> logger)
        {
            this.githubMetaService = githubMetaService;
            this.githubDelegate = githubDelegate;
            this.logger = logger;
        }

        public async Task ScanAddressAsync(CancellationToken cancellationToken = default)
        {
            var meta = await this.githubMetaService.GetMetaAsync(cancellationToken);
            if (meta != null)
            {
                var scanTasks = this.GetMetaScanTasks(meta);
                var contexts = await Task.WhenAll(scanTasks);

                var sortedContexts = contexts
                    .Where(item => item.HttpElapsed != null)
                    .OrderBy(item => item.Domain)
                    .ThenBy(item => item.HttpElapsed);

                using var fileStream = File.OpenWrite("github.txt");
                using var fileWriter = new StreamWriter(fileStream);

                foreach (var context in sortedContexts)
                {
                    var message = context.ToString();
                    await fileWriter.WriteLineAsync(message);
                }
            }

            this.logger.LogInformation("扫描结束");
        }


        private IEnumerable<Task<GithubContext>> GetMetaScanTasks(Meta meta)
        {
            foreach (var item in meta.ToDomainAddress())
            {
                var context = new GithubContext
                {
                    Domain = item.Domain,
                    Address = item.Address,
                };
                yield return InvokeAsync(context);
            }


            async Task<GithubContext> InvokeAsync(GithubContext context)
            {
                await this.githubDelegate(context);
                return context;
            }
        }
    }
}
