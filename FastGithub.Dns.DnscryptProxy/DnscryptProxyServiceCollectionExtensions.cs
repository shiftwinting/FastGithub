using FastGithub.Dns.DnscryptProxy;
using Microsoft.Extensions.DependencyInjection;

namespace FastGithub
{
    /// <summary>
    ///  DnscryptProxy的服务注册扩展
    /// </summary>
    public static class DnscryptProxyServiceCollectionExtensions
    {
        /// <summary>
        /// 添加DnscryptProxy
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        public static IServiceCollection AddDnscryptProxy(this IServiceCollection services)
        {
            return services.AddHostedService<DnscryptProxyHostedService>();
        }
    }
}
