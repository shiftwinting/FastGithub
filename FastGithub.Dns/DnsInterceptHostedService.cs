using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// dns拦截后台服务
    /// </summary>
    [SupportedOSPlatform("windows")]
    sealed class DnsInterceptHostedService : BackgroundService
    {
        private readonly DnsInterceptor dnsInterceptor;
        private readonly IEnumerable<IConflictValidator> conflictValidators;

        /// <summary>
        /// dns拦截后台服务
        /// </summary> 
        /// <param name="dnsInterceptor"></param>
        /// <param name="conflictValidators"></param>
        public DnsInterceptHostedService(
            DnsInterceptor dnsInterceptor,
            IEnumerable<IConflictValidator> conflictValidators)
        {
            this.dnsInterceptor = dnsInterceptor;
            this.conflictValidators = conflictValidators;
        }

        /// <summary>
        /// dns后台
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            foreach (var item in this.conflictValidators)
            {
                await item.ValidateAsync();
            }
            this.dnsInterceptor.Intercept(stoppingToken);
        }
    }
}
