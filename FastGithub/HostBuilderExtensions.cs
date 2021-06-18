using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;
using Topshelf;

namespace FastGithub
{
    static class HostBuilderExtensions
    {
        /// <summary>
        /// topShelf管理运行
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static void RunAsTopShelf(this IHostBuilder hostBuilder)
        {
            if (OperatingSystem.IsWindows())
            {
                HostFactory.Run(c =>
                {
                    var assembly = typeof(HostBuilderExtensions).Assembly;
                    var assemblyName = assembly.GetName().Name;
                    var assemblyDescription = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();

                    c.RunAsLocalSystem();

                    c.SetServiceName(assemblyName);
                    c.SetDisplayName(assemblyName);
                    c.SetDescription(assemblyDescription?.Description);

                    c.Service<IHost>(service => service
                        .ConstructUsing(() => hostBuilder.Build())
                        .WhenStarted(service => service.Start())
                        .WhenStopped(service => service.StopAsync())
                        );
                });
            }
            else
            {
                hostBuilder.Build().Run();
            }
        }
    }
}
