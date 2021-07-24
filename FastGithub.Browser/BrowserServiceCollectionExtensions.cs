using FastGithub.Browser;
using Microsoft.Extensions.DependencyInjection;

namespace FastGithub
{
    /// <summary>
    /// 浏览器服务注册扩展
    /// </summary>
    public static class BrowserServiceCollectionExtensions
    {
        /// <summary>
        /// 注册浏览器服务注册
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        public static IServiceCollection AddBrowser(this IServiceCollection services)
        {
            return services.AddHostedService<BrowserHostedService>();
        }
    }
}
