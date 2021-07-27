using FastGithub.Configuration;

namespace FastGithub.Http
{
    /// <summary>
    /// 表示请求上下文
    /// </summary>
    sealed class RequestContext
    {
        /// <summary>
        /// 获取或设置是否为https请求
        /// </summary>
        public bool IsHttps { get; set; }

        /// <summary>
        /// 请求的主机
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// 获取或设置Sni值的表达式 
        /// </summary>
        public TlsSniPattern TlsSniPattern { get; set; }

        /// <summary>
        /// 是否忽略服务器证书域名不匹配
        /// </summary>
        public bool TlsIgnoreNameMismatch { get; set; }
    }
}
