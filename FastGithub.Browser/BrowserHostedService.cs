using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Browser
{
    /// <summary>
    /// 启动浏览器加载readme
    /// </summary>
    sealed class BrowserHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
           
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
