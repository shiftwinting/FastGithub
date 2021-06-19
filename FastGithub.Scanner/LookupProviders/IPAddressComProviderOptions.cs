namespace FastGithub.Scanner.LookupProviders
{
    /// <summary>
    /// ipaddress.com的域名与ip关系提供者选项
    /// </summary>
    [Options("Github:Lookup:IPAddressComProvider")]
    sealed class IPAddressComProviderOptions
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; }
    }
}
