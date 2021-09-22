using FastGithub.Configuration;
using Microsoft.Extensions.Hosting;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// https拦截后台服务
    /// </summary>
    [SupportedOSPlatform("windows")]
    sealed class HttpsInterceptHostedService : BackgroundService
    {
        private readonly HttpsInterceptor httpsInterceptor;

        /// <summary>
        /// https拦截后台服务
        /// </summary> 
        /// <param name="httpsInterceptor"></param> 
        public HttpsInterceptHostedService(HttpsInterceptor httpsInterceptor)
        {
            this.httpsInterceptor = httpsInterceptor;
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
