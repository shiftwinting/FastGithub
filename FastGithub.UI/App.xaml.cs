using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace FastGithub.UI
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private Mutex globalMutex;
        private Process fastGithub;

        /// <summary>
        /// 程序启动
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            this.globalMutex = new Mutex(true, "Global\\FastGithub.UI", out var firstInstance);
            if (firstInstance == false)
            {
                this.Shutdown();
            }
            else
            {
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
                SetWebBrowserVersion(9000);
                this.fastGithub = StartFastGithub();
            }

            base.OnStartup(e);
        }

        /// <summary>
        /// 程序集加载失败时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            return name.EndsWith(".resources") ? null : LoadAssembly(name);
        }

        /// <summary>
        /// 从资源加载程序集
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Assembly LoadAssembly(string name)
        {
            var stream = GetResourceStream(new Uri($"Resource/{name}.dll", UriKind.Relative)).Stream;
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return Assembly.Load(buffer);
        }

        /// <summary>
        /// 设置浏览器版本
        /// </summary>
        /// <param name="version"></param>
        private static void SetWebBrowserVersion(int version)
        {
            var registry = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true);
            var key = $"{Process.GetCurrentProcess().ProcessName}.exe";
            registry.SetValue(key, version, RegistryValueKind.DWord);
        }

        /// <summary>
        /// 启动fastgithub
        /// </summary>
        /// <returns></returns>
        private static Process StartFastGithub()
        {
            const string fileName = "fastgithub.exe";
            if (File.Exists(fileName) == false)
            {
                return default;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            return Process.Start(startInfo);
        }

        /// <summary>
        /// 程序退出
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            this.globalMutex.Dispose();
            if (this.fastGithub != null && this.fastGithub.HasExited == false)
            {
                this.fastGithub.Kill();
            }
            base.OnExit(e);
        }
    }
}
