using FastGithub.Dns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.Versioning;

namespace FastGithub
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册数据包拦截器
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        public static IServiceCollection AddPacketIntercept(this IServiceCollection services)
        {
            services.AddSingleton<IConflictSolver, HostsConflictSolver>();
            services.AddSingleton<IConflictSolver, ProxyConflictSolver>();

            services.TryAddSingleton<DnsInterceptor>();
            services.TryAddSingleton<HttpInterceptor>();
            services.TryAddSingleton<HttpsInterceptor>();

            services.AddHostedService<DnsInterceptHostedService>();
            services.AddHostedService<HttpInterceptHostedService>();
            return services.AddHostedService<HttpsInterceptHostedService>();
        }
    }
}
