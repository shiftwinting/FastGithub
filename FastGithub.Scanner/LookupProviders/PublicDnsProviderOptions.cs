using System;

namespace FastGithub.Scanner.LookupProviders
{
    /// <summary>
    /// 公共dns的域名与ip关系提供者选项
    /// </summary>
    [Options("Github:Lookup:PublicDnsProvider")]
    sealed class PublicDnsProviderOptions
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// dns列表
        /// </summary>
        public string[] Dnss { get; set; } = Array.Empty<string>();
    }
}
