using Microsoft.AspNetCore.Hosting;
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
            CreateHostBuilder(args).Build().RunWithWindowsServiceControl();
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
                .UseWindowsService()
                .UseBinaryPathContentRoot()
                .UseDefaultServiceProvider(c =>
                {
                    c.ValidateOnBuild = false;
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.AddAppUpgrade();
                    services.AddDnsServer();
                    services.AddReverseProxy();
                    services.AddDnscryptProxy();
                    services.AddSingleton<FastGithubConfig>();
                    services.Configure<FastGithubOptions>(ctx.Configuration.GetSection(nameof(FastGithub)));
                })
                .ConfigureWebHostDefaults(web =>
                {
                    web.Configure(app => app.UseHttpsReverseProxy("README.html"));
                    web.UseKestrel(kestrel => kestrel.ListenHttpsReverseProxy());
                });
        }
    }
}
