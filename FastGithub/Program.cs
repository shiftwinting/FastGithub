using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

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
                .ConfigureAppConfiguration(c =>
                {
                    foreach (var jsonFile in Directory.GetFiles(".", "appsettings.*.json"))
                    {
                        c.AddJsonFile(Path.GetFileName(jsonFile), true, true);
                    }
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.AddDnsServer();
                    services.AddReverseProxy();
                    services.AddDnscryptProxy();
                    services.AddAppUpgrade();
                    services.AddSingleton<FastGithubConfig>();
                    services.Configure<FastGithubOptions>(ctx.Configuration.GetSection(nameof(FastGithub)));
                })
                .ConfigureWebHostDefaults(web =>
                {
                    web.Configure(app =>
                    {
                        app.UseRequestLogging();
                        app.UseHttpsReverseProxy("README.html");
                    });
                    web.UseKestrel(kestrel => kestrel.ListenHttpsReverseProxy());
                });
        }
    }
}
