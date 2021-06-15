using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class GithubScanService
    {
        private readonly GithubMetaService metaService;
        private readonly GithubScanDelegate scanDelegate;
        private readonly ILogger<GithubScanService> logger;
        private readonly GithubContextHashSet results = new();

        public GithubScanService(
            GithubMetaService metaService,
            GithubScanDelegate scanDelegate,
            ILogger<GithubScanService> logger)
        {
            this.metaService = metaService;
            this.scanDelegate = scanDelegate;
            this.logger = logger;
        }

        public async Task ScanAllAsync(CancellationToken cancellationToken = default)
        {
            var meta = await this.metaService.GetMetaAsync(cancellationToken);
            if (meta != null)
            {
                var scanTasks = meta.ToGithubContexts().Select(ctx => ScanAsync(ctx));
                await Task.WhenAll(scanTasks);
            }

            this.logger.LogInformation("完全扫描完成");

            async Task ScanAsync(GithubContext context)
            {
                await this.scanDelegate(context);
                if (context.HttpElapsed != null)
                {
                    lock (this.results.SyncRoot)
                    {
                        this.results.Add(context);
                    }
                }
            }
        }

        public async Task ScanResultAsync()
        {
            GithubContext[] contexts;
            lock (this.results.SyncRoot)
            {
                contexts = this.results.ToArray();
            }

            foreach (var context in contexts)
            {
                await this.scanDelegate(context);
            }

            this.logger.LogInformation("结果扫描完成");
        }

        public IPAddress[] FindAddress(string domain)
        {
            lock (this.results.SyncRoot)
            {
                return this.results
                    .Where(item => item.Domain == domain && item.HttpElapsed != null)
                    .OrderBy(item => item.HttpElapsed)
                    .Select(item => item.Address)
                    .ToArray();
            }
        }
    }
}
