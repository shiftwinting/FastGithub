namespace FastGithub.Scanner.LookupProviders
{
    [Options("Github:Lookup:IPAddressComProvider")]
    sealed class IPAddressComProviderOptions
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; }
    }
}
