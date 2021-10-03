using FastGithub.Configuration;

namespace FastGithub.Http
{
    /// <summary>
    /// 生命周期的Key
    /// </summary>
    record LifeTimeKey
    {
        /// <summary>
        /// 域名
        /// </summary>
        public string Domain { get; }

        /// <summary>
        /// 域名配置
        /// </summary>
        public DomainConfig DomainConfig { get; }

        /// <summary>
        /// 生命周期的Key
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="domainConfig"></param>
        public LifeTimeKey(string domain, DomainConfig domainConfig)
        {
            this.Domain = domain;
            this.DomainConfig = domainConfig;
        }
    }
}
