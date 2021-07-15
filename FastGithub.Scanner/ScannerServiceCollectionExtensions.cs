using FastGithub.Scanner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http.Headers;

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
            var defaultUserAgent = new ProductInfoHeaderValue(assembly.GetName().Name ?? nameof(FastGithub), assembly.GetName().Version?.ToString());

            services
                .AddHttpClient(nameof(Scanner))
                .SetHandlerLifetime(TimeSpan.FromMinutes(5d))
                .ConfigureHttpClient(httpClient =>
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10d);
                    httpClient.DefaultRequestHeaders.Accept.TryParseAdd("*/*");
                    httpClient.DefaultRequestHeaders.UserAgent.Add(defaultUserAgent);
                })
                .ConfigurePrimaryHttpMessageHandler((serviceProvider) =>
                {
                    return serviceProvider.GetRequiredService<GithubHttpClientHanlder>();
                });

            return services
                .AddMemoryCache()
                .AddServiceAndOptions(assembly, configuration)
                .AddHostedService<GithubFullScanHostedService>()
                .AddHostedService<GithubResultScanHostedService>();
            ;
        }
    }
}
