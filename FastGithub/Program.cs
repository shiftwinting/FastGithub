using FastGithub.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace FastGithub
{
    class Program
    {
        /// <summary>
        /// 程序入口
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// 创建host
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureServices((ctx, services) =>
                {
                    services
                        .Configure<DnsOptions>(ctx.Configuration.GetSection("Dns"))
                        .Configure<GithubOptions>(ctx.Configuration.GetSection("Github"))
                        .AddHttpClient()
                        .AddSingleton<GithubMetaService>()
                        .AddSingleton<GithubScanService>()

                        .AddSingleton<PortScanMiddleware>()
                        .AddSingleton<HttpsScanMiddleware>()
                        .AddSingleton<ConcurrentMiddleware>()
                        .AddSingleton<ScanOkLogMiddleware>()
                        .AddSingleton(serviceProvider =>
                        {
                            return new GithubScanBuilder(serviceProvider, ctx => Task.CompletedTask)
                                .Use<ConcurrentMiddleware>()
                                .Use<PortScanMiddleware>()
                                .Use<HttpsScanMiddleware>()
                                .Use<ScanOkLogMiddleware>()
                                .Build();
                        })
                        .AddHostedService<DnsHostedService>()
                        .AddHostedService<GithubScanAllHostedService>()
                        .AddHostedService<GithubScanResultHostedService>()
                        ;
                });

        }
    }
}
