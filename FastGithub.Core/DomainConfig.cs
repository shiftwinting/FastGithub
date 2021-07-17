using System;

namespace FastGithub
{
    /// <summary>
    /// 域名配置
    /// </summary>
    public class DomainConfig
    {
        /// <summary>
        /// 是否发送SNI
        /// </summary>
        public bool TlsSni { get; set; }

        /// <summary>
        /// 请求超时时长
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// 目的地
        /// 格式为相对或绝对uri
        /// </summary>
        public Uri? Destination { get; set; }
    }
}
