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
                    services
                        .AddAppUpgrade()
                        .AddDnsServer()
                        .AddReverseProxy()
                        .AddDnscryptProxy()
                        .AddOptions<FastGithubOptions>()
                            .Bind(ctx.Configuration.GetSection(nameof(FastGithub)))
                            .PostConfigure(opt => opt.Validate());
                })
                .ConfigureWebHostDefaults(web =>
                {
                    web.Configure(app => app.UseHttpsReverseProxy());
                    web.UseKestrel(kestrel => kestrel.ListenHttpsReverseProxy("FastGithub.cer", "FastGithub.key"));
                });
        }
    }
}
