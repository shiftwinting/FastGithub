using System;

namespace FastGithub.Scanner.ScanMiddlewares
{
    [Options("Github:Scan:TcpScan")]
    sealed class TcpScanOptions
    {
        /// <summary>
        /// 扫描超时时长
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1d);

        /// <summary>
        /// 扫描结果缓存时长
        /// </summary>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(20d);
    }
}
