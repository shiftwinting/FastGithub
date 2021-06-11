using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class GithubHostedService : IHostedService
    {
        private readonly IServiceScopeFactory serviceScopeFactory;

        public GithubHostedService(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var scope = this.serviceScopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<GithubService>();
            return service.ScanAddressAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
