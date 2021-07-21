using FastGithub.ReverseProxy;
using Microsoft.Extensions.DependencyInjection;

namespace FastGithub
{
    /// <summary>
    /// https反向代理的服务注册扩展
    /// </summary>
    public static class ReverseProxyServiceCollectionExtensions
    {
        /// <summary>
        /// 添加https反向代理
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        public static IServiceCollection AddReverseProxy(this IServiceCollection services)
        {
            return services
                .AddMemoryCache()
                .AddHttpForwarder()
                .AddSingleton<DomainResolver>()
                .AddTransient<HttpClientFactory>()
                .AddSingleton<RequestLoggingMilldeware>()
                .AddSingleton<ReverseProxyMiddleware>();
        }
    }
}
