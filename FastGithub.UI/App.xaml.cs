using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;

namespace FastGithub.UI
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var emulation = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true);
            var key = $"{Process.GetCurrentProcess().ProcessName}.exe";
            emulation.SetValue(key, 9000, RegistryValueKind.DWord);
            base.OnStartup(e);
        }
    }
}
