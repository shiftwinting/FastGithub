using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.PacketIntercept
{
    /// <summary>
    /// dns拦截后台服务
    /// </summary>
    [SupportedOSPlatform("windows")]
    sealed class DnsInterceptHostedService : BackgroundService
    {
        private readonly DnsInterceptor dnsInterceptor;
        private readonly IEnumerable<IConflictSolver> conflictSolvers;

        /// <summary>
        /// dns拦截后台服务
        /// </summary> 
        /// <param name="dnsInterceptor"></param>
        /// <param name="conflictSolvers"></param>
        public DnsInterceptHostedService(
            DnsInterceptor dnsInterceptor,
            IEnumerable<IConflictSolver> conflictSolvers)
        {
            this.dnsInterceptor = dnsInterceptor;
            this.conflictSolvers = conflictSolvers;
        }

        /// <summary>
        /// 启动时处理冲突
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var solver in this.conflictSolvers)
            {
                await solver.SolveAsync(cancellationToken);
            }
            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// 停止时恢复冲突
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var solver in this.conflictSolvers)
            {
                await solver.RestoreAsync(cancellationToken);
            }
            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// dns后台
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            this.dnsInterceptor.Intercept(stoppingToken);
        }
    }
}
