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
        /// 请求的域名
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// 获取或设置Sni值
        /// </summary>
        public TlsSniPattern TlsSniValue { get; set; }
    }
}
