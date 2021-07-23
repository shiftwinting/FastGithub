using FastGithub.Upgrade;
using Microsoft.Extensions.DependencyInjection;

namespace FastGithub
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class DnsServiceCollectionExtensions
    {
        /// <summary>
        /// 注册升级后台服务
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        public static IServiceCollection AddAppUpgrade(this IServiceCollection services)
        {
            return services
                .AddHttpClient()
                .AddSingleton<UpgradeService>()
                .AddHostedService<UpgradeHostedService>();
        }
    }
}
