using FastGithub.Dns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FastGithub
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册dns服务
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        public static IServiceCollection AddDnsServer(this IServiceCollection services)
        {
            services.TryAddSingleton<RequestResolver>();
            services.TryAddSingleton<DnsOverUdpServer>();
            services.TryAddSingleton<DnsOverHttpsMiddleware>();
            services.AddSingleton<IConflictValidator, HostsConflictValidator>();
            services.AddSingleton<IConflictValidator, ProxyConflictValidtor>();
            return services.AddHostedService<DnsHostedService>();
        }
    }
}
