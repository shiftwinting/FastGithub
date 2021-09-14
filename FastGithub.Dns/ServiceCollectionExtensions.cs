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
        /// 注册dns投毒服务
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        public static IServiceCollection AddDnsPoisoning(this IServiceCollection services)
        { 
            services.TryAddSingleton<DnsPoisoningServer>();
            services.AddSingleton<IConflictValidator, HostsConflictValidator>();
            services.AddSingleton<IConflictValidator, ProxyConflictValidtor>();
            return services.AddHostedService<DnsDnsPoisoningHostedService>();
        }
    }
}
