using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace FastGithub.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private const string FAST_GITHUB = "FastGithub";
        private const string PROJECT_URI = "https://github.com/dotnetcore/FastGithub";

        public MainWindow()
        {
            InitializeComponent();

            var about = new System.Windows.Forms.MenuItem("关于(&A)");
            about.Click += (s, e) => Process.Start(PROJECT_URI);

            var exit = new System.Windows.Forms.MenuItem("退出(&C)");
            exit.Click += (s, e) => this.Close();

            this.notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Visible = true,
                Text = FAST_GITHUB,
                ContextMenu = new System.Windows.Forms.ContextMenu(new[] { about, exit }),
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath)
            };

            this.notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    this.Show();
                    this.Activate();
                    this.WindowState = WindowState.Normal;
                }
            };

            var fileName = $"{FAST_GITHUB}.exe";
            if (File.Exists(fileName) == true)
            {
                var version = FileVersionInfo.GetVersionInfo(fileName);
                this.Title = $"{FAST_GITHUB} v{version.ProductVersion}";
            }

            this.webBrowserIssue.AddHandler(KeyDownEvent, new RoutedEventHandler(WebBrowser_KeyDown), true);
            var resource = Application.GetResourceStream(new Uri("Resource/issue.html", UriKind.Relative));
            this.webBrowserIssue.NavigateToStream(resource.Stream);
        }

        /// <summary>
        /// 拦截F5
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebBrowser_KeyDown(object sender, RoutedEventArgs e)
        {
            var @event = (KeyEventArgs)e;
            if (@event.Key == Key.F5)
            {
                var resource = Application.GetResourceStream(new Uri("Resource/issue.html", UriKind.Relative));
                this.webBrowserIssue.NavigateToStream(resource.Stream);
            }
        }

        /// <summary>
        /// 拦截最小化事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            hwndSource.AddHook(WndProc);

            IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                const int WM_SYSCOMMAND = 0x112;
                const int SC_MINIMIZE = 0xf020;

                if (msg == WM_SYSCOMMAND && wParam.ToInt32() == SC_MINIMIZE)
                {
                    this.Hide();
                    handled = true;
                }
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 关闭时
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            this.notifyIcon.Icon = null;
            this.notifyIcon.Dispose();
            base.OnClosed(e);
        }
    }
}
