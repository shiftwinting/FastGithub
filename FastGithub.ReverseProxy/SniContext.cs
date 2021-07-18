namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// Sni上下文
    /// </summary>
    sealed class SniContext
    {
        /// <summary>
        /// 获取请求是否为https
        /// </summary>
        public bool IsHttps { get; }

        /// <summary>
        /// 获取是否发送Sni
        /// </summary>
        public bool TlsSni { get; }

        /// <summary>
        /// Sni值
        /// </summary>
        public string TlsSniValue { get; set; } = string.Empty;

        /// <summary>
        /// Sni上下文
        /// </summary>
        /// <param name="isHttps"></param>
        /// <param name="tlsSni"></param>
        public SniContext(bool isHttps, bool tlsSni)
        {
            this.IsHttps = isHttps;
            this.TlsSni = tlsSni;
        }
    }
}
