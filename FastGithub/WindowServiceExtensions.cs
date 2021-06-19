using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PInvoke;
using System;
using System.IO;
using System.Linq;
using static PInvoke.AdvApi32;

namespace FastGithub
{
    /// <summary>
    /// IHostBuilder扩展
    /// </summary>
    static class WindowServiceExtensions
    {
        /// <summary>
        /// 控制命令
        /// </summary>
        private enum Command
        {
            Start,
            Stop,
        }

        /// <summary>
        /// 使用应用程序文件所在目录作为ContentRoot
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseBinaryPathContentRoot(this IHostBuilder hostBuilder)
        {
            var contentRoot = Path.GetDirectoryName(Environment.GetCommandLineArgs().First());
            if (contentRoot != null)
            {
                Environment.CurrentDirectory = contentRoot;
                hostBuilder.UseContentRoot(contentRoot);
            }
            return hostBuilder;
        }

        /// <summary>
        /// 以支持windows服务控制的方式运行
        /// </summary>
        /// <param name="host"></param>
        public static void RunWithWindowsServiceControl(this IHost host)
        {
            var args = Environment.GetCommandLineArgs();
            if (OperatingSystem.IsWindows() == false ||
                Enum.TryParse<Command>(args.Skip(1).FirstOrDefault(), true, out var cmd) == false)
            {
                host.Run();
                return;
            }

            try
            {
                var binaryPath = args.First();
                var serviceName = Path.GetFileNameWithoutExtension(binaryPath);

                if (cmd == Command.Start)
                {
                    InstallAndStartService(serviceName, binaryPath);
                }
                else if (cmd == Command.Stop)
                {
                    StopAndDeleteService(serviceName);
                }
            }
            catch (Exception ex)
            {
                var loggerFactory = host.Services.GetService<ILoggerFactory>();
                if (loggerFactory != null)
                {
                    var logger = loggerFactory.CreateLogger(nameof(WindowServiceExtensions));
                    logger.LogError(ex.Message);
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// 安装并启动服务
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="binaryPath"></param>
        /// <exception cref = "Win32Exception" ></ exception >
        private static void InstallAndStartService(string serviceName, string binaryPath)
        {
            using var hSCManager = OpenSCManager(null, null, ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
            if (hSCManager.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            var hService = OpenService(hSCManager, serviceName, ServiceAccess.SERVICE_ALL_ACCESS);
            if (hService.IsInvalid == true)
            {
                hService = CreateService(
                    hSCManager,
                    serviceName,
                    serviceName,
                    ServiceAccess.SERVICE_ALL_ACCESS,
                    ServiceType.SERVICE_WIN32_OWN_PROCESS,
                    ServiceStartType.SERVICE_AUTO_START,
                    ServiceErrorControl.SERVICE_ERROR_NORMAL,
                    binaryPath,
                    lpLoadOrderGroup: null,
                    lpdwTagId: 0,
                    lpDependencies: null,
                    lpServiceStartName: null,
                    lpPassword: null);
            }

            if (hService.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            using (hService)
            {
                StartService(hService, 0, null);
            }
        }

        /// <summary>
        /// 停止并删除服务
        /// </summary>
        /// <param name="serviceName"></param>
        /// <exception cref = "Win32Exception" ></ exception >
        private static void StopAndDeleteService(string serviceName)
        {
            using var hSCManager = OpenSCManager(null, null, ServiceManagerAccess.SC_MANAGER_ALL_ACCESS);
            if (hSCManager.IsInvalid == true)
            {
                throw new Win32Exception();
            }

            using var hService = OpenService(hSCManager, serviceName, ServiceAccess.SERVICE_ALL_ACCESS);
            if (hService.IsInvalid == true)
            {
                return;
            }

            var status = new SERVICE_STATUS();
            if (QueryServiceStatus(hService, ref status) == true)
            {
                if (status.dwCurrentState != ServiceState.SERVICE_STOP_PENDING &&
                    status.dwCurrentState != ServiceState.SERVICE_STOPPED)
                {
                    ControlService(hService, ServiceControl.SERVICE_CONTROL_STOP, ref status);
                }
            }

            if (DeleteService(hService) == false)
            {
                throw new Win32Exception();
            }
        }

    }
}
