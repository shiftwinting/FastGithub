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
                .ConfigureServices((ctx, services) =>
                {
                    services.AddHostedService<VersionHostedService>();
                    services.AddGithubDns(ctx.Configuration);
                });
        }
    }
}
