using System;

namespace FastGithub.Scanner.LookupProviders
{
    /// <summary>
    /// 公共dns的域名与ip关系提供者选项
    /// </summary>
    [Options("Lookup:PublicDnsProvider")]
    sealed class PublicDnsProviderOptions
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// dns查询超时时长
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(100d);

        /// <summary>
        /// dns列表
        /// </summary>
        public string[] Dnss { get; set; } = Array.Empty<string>();
    }
}
