using FastGithub.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                        .AddSingleton<PortScanMiddleware>()
                        .AddSingleton<HttpTestMiddleware>()
                        .AddSingleton<ConcurrentMiddleware>()
                        .AddHostedService<GithubHostedService>()
                        ;
                });

        }
    }
}
