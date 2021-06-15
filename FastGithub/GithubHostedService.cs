using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class GithubHostedService : IHostedService
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IHostApplicationLifetime hostApplicationLifetime;

        public GithubHostedService(
            IServiceScopeFactory serviceScopeFactory,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.hostApplicationLifetime = hostApplicationLifetime;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var scope = this.serviceScopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<GithubService>();
            await service.ScanAddressAsync(cancellationToken);
            this.hostApplicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
