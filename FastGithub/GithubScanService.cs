using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class GithubScanService
    {
        private readonly GithubMetaService metaService;
        private readonly GithubScanDelegate scanDelegate;

        public ConcurrentQueue<GithubContext> Result { get; } = new();

        public GithubScanService(
            GithubMetaService metaService,
            GithubScanDelegate scanDelegate)
        {
            this.metaService = metaService;
            this.scanDelegate = scanDelegate;
        }

        public async Task ScanAllAsync(CancellationToken cancellationToken = default)
        {
            var meta = await this.metaService.GetMetaAsync(cancellationToken);
            if (meta != null)
            {
                this.Result.Clear();
                var scanTasks = meta.ToGithubContexts().Select(ctx => ScanAsync(ctx));
                await Task.WhenAll(scanTasks);
            }
        }

        public async Task ScanResultAsync()
        {
            while (this.Result.TryDequeue(out var context))
            {
                await this.ScanAsync(context);
            }
        }

        private async Task ScanAsync(GithubContext context)
        {
            await this.scanDelegate(context);
            if (context.HttpElapsed != null)
            {
                this.Result.Enqueue(context);
            }
        }
    }
}
