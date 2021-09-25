using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PInvoke;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
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
        /// 使用windows服务
        /// </summary>
        /// <param name="hostBuilder"></param> 
        /// <returns></returns>
        public static IHostBuilder UseWindowsService(this IHostBuilder hostBuilder)
        {
            var contentRoot = Path.GetDirectoryName(Environment.GetCommandLineArgs().First());
            if (contentRoot != null)
            {
                Environment.CurrentDirectory = contentRoot;
                hostBuilder.UseContentRoot(contentRoot);
            }
            return WindowsServiceLifetimeHostBuilderExtensions.UseWindowsService(hostBuilder);
        }

        /// <summary>
        /// 运行主机
        /// </summary>
        /// <param name="host"></param>
        /// <param name="singleton"></param>
        public static void Run(this IHost host, bool singleton = true)
        {
            if (OperatingSystem.IsWindows() && TryGetCommand(out var cmd))
            {
                try
                {
                    UseCommand(cmd);
                }
                catch (Exception ex)
                {
                    var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
                    loggerFactory.CreateLogger(nameof(FastGithub)).LogError(ex.Message);
                }
            }
            else
            {
                using var mutex = new Mutex(true, "Global\\FastGithub", out var firstInstance);
                if (singleton == false || firstInstance)
                {
                    HostingAbstractionsHostExtensions.Run(host);
                }
            }
        }

        /// <summary>
        /// 获取控制指令
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private static bool TryGetCommand(out Command cmd)
        {
            var args = Environment.GetCommandLineArgs();
            return Enum.TryParse(args.Skip(1).FirstOrDefault(), true, out cmd);
        }

        /// <summary>
        /// 应用控制指令
        /// </summary> 
        /// <param name="cmd"></param>
        [SupportedOSPlatform("windows")]
        private static void UseCommand(Command cmd)
        {
            var binaryPath = Environment.GetCommandLineArgs().First();
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

        /// <summary>
        /// 安装并启动服务
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="binaryPath"></param>
        /// <exception cref = "Win32Exception" ></ exception >
        [SupportedOSPlatform("windows")]
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
        [SupportedOSPlatform("windows")]
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
