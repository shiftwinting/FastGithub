using FastGithub.Configuration;
using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// 启动项
    /// </summary>
    public class Startup
    {
        public IConfiguration Configuration { get; }

        /// <summary>
        /// 启动项
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// 配置服务
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<FastGithubOptions>(this.Configuration.GetSection(nameof(FastGithub)));

            services.AddConfiguration();
            services.AddDomainResolve();
            services.AddHttpClient();
            services.AddReverseProxy();
            services.AddHostedService<VersonHostedService>();

            if (OperatingSystem.IsWindows())
            {
                services.AddDnsPoisoning();
            }
        }

        /// <summary>
        /// 配置中间件
        /// </summary>
        /// <param name="app"></param>
        public void Configure(IApplicationBuilder app)
        {
            if (OperatingSystem.IsWindows())
            {
                app.UseRequestLogging();
                app.UseHttpReverseProxy();
                app.UseRouting();
                app.UseEndpoints(endpoint => endpoint.MapFallback(context =>
                {
                    context.Response.Redirect("https://github.com/dotnetcore/FastGithub");
                    return Task.CompletedTask;
                }));
            }
            else
            {
                var portService = app.ApplicationServices.GetRequiredService<PortService>();
                app.MapWhen(context => context.Connection.LocalPort == portService.HttpProxyPort, appBuilder =>
                {
                    appBuilder.UseHttpProxy();
                });

                app.MapWhen(context => context.Connection.LocalPort != portService.HttpProxyPort, appBuilder =>
                {
                    appBuilder.UseRequestLogging();
                    appBuilder.UseHttpReverseProxy();
                });
            }
        }
    }
}
