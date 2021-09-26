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
        public bool IsHttps { get; }

        /// <summary>
        /// 获取或设置Sni值
        /// </summary>
        public TlsSniPattern TlsSniValue { get; }

        /// <summary>
        /// 请求上下文
        /// </summary>
        /// <param name="isHttps"></param>
        /// <param name="tlsSniValue"></param>
        public RequestContext(bool isHttps, TlsSniPattern tlsSniValue)
        {
            IsHttps = isHttps;
            TlsSniValue = tlsSniValue;
        }
    }
}
