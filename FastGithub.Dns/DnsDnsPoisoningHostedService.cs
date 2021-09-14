using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// dns投毒后台服务
    /// </summary>
    [SupportedOSPlatform("windows")]
    sealed class DnsDnsPoisoningHostedService : BackgroundService
    {
        private readonly DnsPoisoningServer dnsPoisoningServer;
        private readonly IEnumerable<IConflictValidator> conflictValidators;

        /// <summary>
        /// dns后台服务
        /// </summary> 
        /// <param name="dnsPoisoningServer"></param>
        /// <param name="conflictValidators"></param>
        public DnsDnsPoisoningHostedService(
            DnsPoisoningServer dnsPoisoningServer,
            IEnumerable<IConflictValidator> conflictValidators)
        {
            this.dnsPoisoningServer = dnsPoisoningServer;
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

            if (OperatingSystem.IsWindows())
            {
                foreach (var item in this.conflictValidators)
                {
                    await item.ValidateAsync();
                }
                this.dnsPoisoningServer.DnsPoisoning(stoppingToken);
            }
        }
    }
}
