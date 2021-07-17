using System;

namespace FastGithub
{
    /// <summary>
    /// 域名配置
    /// </summary>
    public class DomainConfig
    {
        /// <summary>
        /// 是否不发送SNI
        /// </summary>
        public bool NoSni { get; set; } = true;

        /// <summary>
        /// 目的地
        /// 格式为相对或绝对uri
        /// </summary>
        public Uri? Destination { get; set; }
    }
}
