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
                        .Configure<GithubOptions>(ctx.Configuration.GetSection("Github"))
                        .AddHttpClient()
                        .AddTransient<GithubMetaService>()
                        .AddTransient<GithubService>()

                        .AddSingleton<PortScanMiddleware>()
                        .AddSingleton<HttpsScanMiddleware>()
                        .AddSingleton<ConcurrentMiddleware>()
                        .AddSingleton(serviceProvider =>
                        {
                            return new GithubBuilder(serviceProvider, ctx => Task.CompletedTask)
                                .Use<ConcurrentMiddleware>()
                                .Use<PortScanMiddleware>()
                                .Use<HttpsScanMiddleware>()
                                .Build();
                        })
                        .AddHostedService<GithubHostedService>()
                        ;
                });

        }
    }
}
