using System;
using System.Windows;

namespace FastGithub.UI
{
    public class UdpLog
    {
        public DateTime Timestamp { get; set; }

        public LogLevel Level { get; set; }

        public string Message { get; set; } = string.Empty;

        public string SourceContext { get; set; } = string.Empty;

        public string Color => this.Level <= LogLevel.Information ? "#333" : "IndianRed";

        public void SetToClipboard()
        {
            Clipboard.SetText($"{this.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\r\n{this.Message}");
        }
    }

}
