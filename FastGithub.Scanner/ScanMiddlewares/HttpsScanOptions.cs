using System;
using System.Collections.Generic;

namespace FastGithub.Scanner.ScanMiddlewares
{
    /// <summary>
    /// https扫描选项
    /// </summary>
    [Options("Scan:HttpsScan")]
    sealed class HttpsScanOptions
    {
        /// <summary>
        /// 扫描超时时长
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5d);

        /// <summary>
        /// 是否使用短连接
        /// </summary>
        public bool ConnectionClose { get; set; } = false;

        /// <summary>
        /// 各域名扫描规则
        /// </summary>
        public Dictionary<string, ScanRule> Rules { get; set; } = new Dictionary<string, ScanRule>();

        /// <summary>
        /// 扫描规则
        /// </summary>
        public class ScanRule
        {
            /// <summary>
            /// 请求方式
            /// </summary>
            public string Method { get; set; } = "HEAD";

            /// <summary>
            /// 请求路径
            /// </summary>
            public string Path { get; set; } = "/";
        }
    }
}
