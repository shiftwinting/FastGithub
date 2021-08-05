using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
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
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseShutdownTimeout(TimeSpan.FromSeconds(2d));
                    webBuilder.UseKestrel(kestrel =>
                    {
                        kestrel.Limits.MaxRequestBodySize = null;
                        kestrel.ListenHttpReverseProxy();
                        kestrel.ListenHttpsReverseProxy();
                    });
                });
        }
    }
}
