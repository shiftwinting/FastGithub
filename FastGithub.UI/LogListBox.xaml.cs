using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Controls;

namespace FastGithub.UI
{
    /// <summary>
    /// LogListBox.xaml 的交互逻辑
    /// </summary>
    public partial class LogListBox : UserControl
    {
        private readonly byte[] buffer = new byte[ushort.MaxValue];
        private readonly Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        public ObservableCollection<UdpLog> LogList { get; } = new ObservableCollection<UdpLog>();

        public LogListBox()
        {
            InitializeComponent();
            DataContext = this;

            this.socket.Bind(new IPEndPoint(IPAddress.Loopback, UdpLoggerPort.Value));
            this.BeginReceiveFrom();
        }

        private void BeginReceiveFrom()
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            this.socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP, this.EndReceiveFrom, null);
        }

        private void EndReceiveFrom(IAsyncResult ar)
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            var length = this.socket.EndReceiveFrom(ar, ref remoteEP);
            var json = Encoding.UTF8.GetString(buffer, 0, length);
            var log = Newtonsoft.Json.JsonConvert.DeserializeObject<UdpLog>(json);
            this.Dispatcher.Invoke(() => this.LogList.Add(log));
            this.BeginReceiveFrom();
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
