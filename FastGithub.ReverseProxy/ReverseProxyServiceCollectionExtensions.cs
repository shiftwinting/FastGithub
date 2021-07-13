using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FastGithub
{
    /// <summary>
    /// gitub反向代理的服务注册扩展
    /// </summary>
    public static class ReverseProxyServiceCollectionExtensions
    {
        /// <summary>
        /// gitub反向代理
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddGithubReverseProxy(this IServiceCollection services, IConfiguration configuration)
        {
            var assembly = typeof(ReverseProxyServiceCollectionExtensions).Assembly;
            return services
                .AddServiceAndOptions(assembly, configuration)
                .AddHttpForwarder();
        }
    }
}
