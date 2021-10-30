namespace FastGithub.UI
{
    public class FlowRate
    {
        /// <summary>
        /// 获取总读上行
        /// </summary>
        public long TotalRead { get; set; }

        /// <summary>
        /// 获取总下行
        /// </summary>
        public long TotalWrite { get; set; }

        public double ReadRate { get; set; }

        public double WriteRate { get; set; }
  

        public string ToTotalReadString()
        {
            return ToNetworkString(this.TotalRead);
        }

        public string ToTotalWriteString()
        {
            return ToNetworkString(this.TotalWrite);
        }
        private static string ToNetworkString(long value)
        {
            if (value < 1024)
            {
                return $"{value}B";
            }
            if (value < 1024 * 1024)
            {
                return $"{value / 1024d:0.00}KB";
            }
            return $"{value / 1024d / 1024d:0.00}MB";
        }
    }
}
