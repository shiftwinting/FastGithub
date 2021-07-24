using FastGithub.DomainResolve;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FastGithub
{
    /// <summary>
    ///  域名解析相关服务注册扩展
    /// </summary>
    public static class DomainResolveServiceCollectionExtensions
    {
        /// <summary>
        /// 注册域名解析相关服务
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        public static IServiceCollection AddDomainResolve(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.TryAddSingleton<IDomainResolver, DomainResolver>();
            services.AddSingleton<DnscryptProxyService>();
            services.AddHostedService<DnscryptProxyHostedService>();
            return services;
        }
    }
}
