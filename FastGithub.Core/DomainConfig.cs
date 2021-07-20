using System;

namespace FastGithub
{
    /// <summary>
    /// 域名配置
    /// </summary>
    public record DomainConfig
    {
        /// <summary>
        /// 是否发送SNI
        /// </summary>
        public bool TlsSni { get; init; }

        /// <summary>
        /// 自定义SNI值的表达式
        /// </summary>
        public string? TlsSniPattern { get; init; }

        /// <summary>
        /// 是否忽略服务器证书域名不匹配
        /// 当不发送SNI时服务器可能发回域名不匹配的证书
        /// </summary>
        public bool TlsIgnoreNameMismatch { get; init; }

        /// <summary>
        /// 请求超时时长
        /// </summary>
        public TimeSpan? Timeout { get; init; }

        /// <summary>
        /// 目的地
        /// 格式为相对或绝对uri
        /// </summary>
        public Uri? Destination { get; init; }

        /// <summary>
        /// 自定义响应
        /// </summary>
        public ResponseConfig? Response { get; init; }

        /// <summary>
        /// 获取TlsSniPattern
        /// </summary>
        /// <returns></returns>
        public TlsSniPattern GetTlsSniPattern()
        {
            if (this.TlsSni == false)
            {
                return FastGithub.TlsSniPattern.None;
            }
            if (string.IsNullOrEmpty(this.TlsSniPattern))
            {
                return FastGithub.TlsSniPattern.Domain;
            }
            return new TlsSniPattern(this.TlsSniPattern);
        }
    }
}
