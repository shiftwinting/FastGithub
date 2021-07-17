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
        /// 支持ip或域名
        /// 留空则本域名
        /// </summary>
        public string? Destination { get; set; }
    }
}
