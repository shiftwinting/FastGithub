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
        /// 注册dns拦截器
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        public static IServiceCollection AddDnsInterceptor(this IServiceCollection services)
        {
            services.TryAddSingleton<DnsInterceptor>();
            services.AddSingleton<IConflictSolver, HostsConflictSolver>();
            services.AddSingleton<IConflictSolver, ProxyConflictSolver>();
            return services.AddHostedService<DnsInterceptHostedService>();
        }
    }
}
