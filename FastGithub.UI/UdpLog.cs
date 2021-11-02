using System;
using System.Windows;
using System.Windows.Media;

namespace FastGithub.UI
{
    public class UdpLog
    {
        public DateTime Timestamp { get; set; }

        public string Level { get; set; }

        public string Message { get; set; }

        public string SourceContext { get; set; }

        public string Color => this.Level == "Information" ? "#333" : "IndianRed";

        public void SetToClipboard()
        {
            Clipboard.SetText($"{this.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")}\r\n{this.SourceContext}\r\n{this.Message}");
        }
    }
}
