using FastGithub.Scanner;
using FastGithub.Scanner.Middlewares;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class ScannerServiceCollectionExtensions
    {
        /// <summary>
        /// 注册程序集下所有服务下选项
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration">配置</param>  
        /// <returns></returns>
        public static IServiceCollection AddGithubScanner(this IServiceCollection services, IConfiguration configuration)
        {
            var assembly = typeof(ScannerServiceCollectionExtensions).Assembly;
            return services
                .AddHttpClient()
                .AddSingleton(serviceProvider =>
                {
                    return new GithubScanBuilder(serviceProvider, ctx => Task.CompletedTask)
                        .Use<ConcurrentMiddleware>()
                        .Use<PortScanMiddleware>()
                        .Use<HttpsScanMiddleware>()
                        .Use<ScanOkLogMiddleware>()
                        .Build();
                })
                .AddServiceAndOptions(assembly, configuration)
                .AddHostedService<GithubFullScanHostedService>()
                .AddHostedService<GithubResultScanHostedService>()
                ;
        }
    }
}
