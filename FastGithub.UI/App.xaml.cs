using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace FastGithub.UI
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private Mutex mutex;
        private Process fastGithub;

        protected override void OnStartup(StartupEventArgs e)
        {
            this.mutex = new Mutex(true, "Global\\FastGithub.UI", out var firstInstance);
            if (firstInstance == false)
            {
                this.Shutdown();
                return;
            }

            this.fastGithub = StartFastGithub();  
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            this.mutex.Dispose();
            if (this.fastGithub != null && this.fastGithub.HasExited == false)
            {
                this.fastGithub.Kill();
            }
            base.OnExit(e);
        }

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
    }
}
