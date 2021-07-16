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
                    services.AddGithubDns();
                    services.AddGithubReverseProxy();
                    services.AddOptions<FastGithubOptions>()
                        .Bind(ctx.Configuration.GetSection(nameof(FastGithub)))
                        .Validate(opt => opt.TrustedDns.Validate() && opt.UntrustedDns.Validate(), "无效的Dns配置");
                })
                .ConfigureWebHostDefaults(web =>
                {
                    web.Configure(app => app.UseGithubReverseProxy());
                    web.UseKestrel(kestrel => kestrel.ListenGithubReverseProxy("FastGithub.cer", "FastGithub.key"));
                });
        }
    }
}
