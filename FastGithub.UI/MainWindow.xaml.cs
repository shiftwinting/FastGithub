using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace FastGithub.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            var exit = new MenuItem("退出");
            exit.Click += (s, e) => this.Close();

            this.notifyIcon = new NotifyIcon
            {
                Visible = true,
                Text = "FastGithub",
                ContextMenu = new ContextMenu(new[] { exit }),
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath)
            };

            this.notifyIcon.MouseClick += (s, e) => this.Show();

            var fileName = "fastgithub.exe";
            if (File.Exists(fileName))
            {
                var version = FileVersionInfo.GetVersionInfo(fileName);
                this.Title = $"FastGithub v{version.ProductVersion}";
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            hwndSource.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_SYSCOMMAND = 0x112;
            const int SC_CLOSE = 0xf060;
            const int SC_MINIMIZE = 0xf020;

            if (msg == WM_SYSCOMMAND)
            {
                if (wParam.ToInt32() == SC_CLOSE || wParam.ToInt32() == SC_MINIMIZE)
                {
                    this.Hide();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            this.notifyIcon.Icon = null;
            this.notifyIcon.Dispose();
            base.OnClosed(e);
        }
    }
}
