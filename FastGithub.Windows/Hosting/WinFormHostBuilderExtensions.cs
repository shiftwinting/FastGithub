using FastGithub.Windows.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// IHostBuilder的WinForm扩展
    /// </summary>
    public static class WinFormHostBuilderExtensions
    {
        /// <summary>
        /// 指定WinForm的主窗体
        /// </summary>
        /// <remarks>
        /// * 该方法需要在services.AddHostedService()之前调用
        /// </remarks>
        /// <typeparam name="TMainForm"></typeparam>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseWinForm<TMainForm>(this IHostBuilder hostBuilder) where TMainForm : Form
        {
            return hostBuilder.ConfigureServices((context, services) =>
            {
                services
                    .AddSingleton<TMainForm>()
                    .AddSingleton<IWinFormDispatcher, WinFormDispatcher>()
                    .AddHostedService<WinFormHostedService<TMainForm>>();
            });
        }

        /// <summary>
        /// 使用WinForm生命周期
        /// </summary>
        /// <remarks>
        /// * 关闭主窗体或调用Appliaction.Exit()之后生命结束
        /// </remarks>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseWinFormHostLifetime(this IHostBuilder hostBuilder)
        {
            return hostBuilder.UseWinFormHostLifetime(c => { });
        }

        /// <summary>
        /// 使用WinForm生命周期
        /// </summary>
        /// <remarks>
        /// * 关闭主窗体或调用Appliaction.Exit()之后生命结束
        /// </remarks>
        /// <param name="hostBuilder"></param>
        /// <param name="configureOptions">Applicaiton选项</param>
        public static IHostBuilder UseWinFormHostLifetime(this IHostBuilder hostBuilder, Action<ApplicationOptions> configureOptions)
        {
            return hostBuilder.ConfigureServices((context, services) =>
            {
                services.Configure(configureOptions);
                services.AddSingleton<IHostLifetime, WinFormHostLifetime>();
            });
        }
    }
}
