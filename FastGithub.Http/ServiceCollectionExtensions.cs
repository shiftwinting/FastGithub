using FastGithub.Http;
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
        /// 添加HttpClient相关服务
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        public static IServiceCollection AddHttpClient(this IServiceCollection services)
        {
            services.TryAddSingleton<IHttpClientFactory, HttpClientFactory>();
            return services;
        }
    }
}
