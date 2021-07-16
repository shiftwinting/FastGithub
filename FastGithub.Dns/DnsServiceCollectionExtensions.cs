using FastGithub.Dns;
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
        /// <returns></returns>
        public static IServiceCollection AddGithubDns(this IServiceCollection services)
        {
            return services
                .AddSingleton<FastGihubResolver>()
                .AddHostedService<DnsServerHostedService>();
        }
    }
}
