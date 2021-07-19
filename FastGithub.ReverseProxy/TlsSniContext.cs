namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// Sni上下文
    /// </summary>
    sealed class TlsSniContext
    {
        /// <summary>
        /// 获取是否为https请求
        /// </summary>
        public bool IsHttps { get; }

        /// <summary>
        /// 获取或设置Sni值的表达式 
        /// </summary>
        public TlsSniPattern TlsSniPattern { get; set; }

        /// <summary>
        /// Sni上下文
        /// </summary>
        /// <param name="isHttps"></param>
        /// <param name="tlsSniPattern"></param>
        public TlsSniContext(bool isHttps, TlsSniPattern tlsSniPattern)
        {
            this.IsHttps = isHttps;
            this.TlsSniPattern = tlsSniPattern;
        }
    }
}
