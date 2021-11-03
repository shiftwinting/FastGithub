using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FastGithub.UI
{
    /// <summary>
    /// UdpLogListBox.xaml 的交互逻辑
    /// </summary>
    public partial class UdpLogListBox : UserControl
    {
        private readonly byte[] buffer = new byte[ushort.MaxValue];
        private readonly Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        public ObservableCollection<UdpLog> LogList { get; } = new ObservableCollection<UdpLog>();

        public UdpLogListBox()
        {
            InitializeComponent();

            this.DataContext = this;
            this.InitUdpLoggerAsync();
        }

        private async void InitUdpLoggerAsync()
        {
            this.socket.Bind(new IPEndPoint(IPAddress.Loopback, UdpLoggerPort.Value));
            while (this.Dispatcher.HasShutdownStarted == false)
            {
                var log = await this.GetUdpLogAsync();
                if (log != null)
                {
                    this.LogList.Add(log);
                }
            }
        }

        private async Task<UdpLog?> GetUdpLogAsync()
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            var taskCompletionSource = new TaskCompletionSource<int>();
            this.socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP, this.EndReceiveFrom, taskCompletionSource);
            var length = await taskCompletionSource.Task;

            var json = Encoding.UTF8.GetString(buffer, 0, length);
            return JsonConvert.DeserializeObject<UdpLog>(json);
        }

        private void EndReceiveFrom(IAsyncResult ar)
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            var length = this.socket.EndReceiveFrom(ar, ref remoteEP);
            var taskCompletionSource = (TaskCompletionSource<int>)ar.AsyncState;
            taskCompletionSource.TrySetResult(length);
        }

        private void MenuItem_Copy_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.listBox.SelectedValue is UdpLog udpLog)
            {
                udpLog.SetToClipboard();
            }
        }

        private void MenuItem_Clear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.LogList.Clear();
        }
    }
}
