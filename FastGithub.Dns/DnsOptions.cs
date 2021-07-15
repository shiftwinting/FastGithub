using System;

namespace FastGithub.Dns
{
    /// <summary>
    /// dns服务选项
    /// </summary>
    [Options("Dns")]
    sealed class DnsOptions
    {
        /// <summary>
        /// 获取或设置上游ip地址
        /// </summary>
        public string UpStream { get; set; } = "114.114.114.114";

        /// <summary>
        /// 获取或设置github相关域名的ip存活时长
        /// </summary>
        public TimeSpan GithubTTL { get; set; } = TimeSpan.FromMinutes(10d);

        /// <summary>
        /// 是否设置本机使用此dns
        /// </summary>
        public bool SetToLocalMachine { get; set; } = true;

        /// <summary>
        /// 是否使用反向代理访问github
        /// </summary>
        public bool UseGithubReverseProxy { get; set; } = true; 
    }
}
