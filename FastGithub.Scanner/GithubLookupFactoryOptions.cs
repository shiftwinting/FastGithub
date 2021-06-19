using System;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 域名
    /// </summary>
    [Options("Github:Lookup")]
    sealed class GithubLookupFactoryOptions
    {
        /// <summary>
        /// 反查的域名
        /// </summary>
        public string[] Domains { get; set; } = Array.Empty<string>();
    }
}
