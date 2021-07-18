namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// Sni上下文
    /// </summary>
    sealed class SniContext
    {
        /// <summary>
        /// 获取是否为https请求
        /// </summary>
        public bool IsHttps { get; }

        /// <summary>
        /// 获取Sni值
        /// </summary>
        public string TlsSniValue { get; }

        /// <summary>
        /// Sni上下文
        /// </summary>
        /// <param name="isHttps"></param>
        /// <param name="tlsSniValue"></param>
        public SniContext(bool isHttps, string tlsSniValue)
        {
            this.IsHttps = isHttps;
            this.TlsSniValue = tlsSniValue;
        }
    }
}
