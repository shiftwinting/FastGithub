using Microsoft.Extensions.Hosting;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// http拦截后台服务
    /// </summary>
    [SupportedOSPlatform("windows")]
    sealed class HttpInterceptHostedService : BackgroundService
    {
        private readonly HttpInterceptor httpsInterceptor;

        /// <summary>
        /// http拦截后台服务
        /// </summary> 
        /// <param name="httpInterceptor"></param> 
        public HttpInterceptHostedService(HttpInterceptor httpInterceptor)
        {
            this.httpsInterceptor = httpInterceptor;
        }

        /// <summary>
        /// https后台
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            this.httpsInterceptor.Intercept(stoppingToken);
        }
    }
}
