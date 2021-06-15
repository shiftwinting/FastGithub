using FastGithub.Dns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FastGithub
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class DnsServiceCollectionExtensions
    {
        /// <summary>
        /// 注册github的dns服务
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration">配置</param>  
        /// <returns></returns>
        public static IServiceCollection AddGithubDns(this IServiceCollection services, IConfiguration configuration)
        {
            var assembly = typeof(DnsServiceCollectionExtensions).Assembly;
            return services               
                .AddServiceAndOptions(assembly, configuration)
                .AddHostedService<DnsHostedService>()
                .AddGithubScanner(configuration);
        }
    }
}
