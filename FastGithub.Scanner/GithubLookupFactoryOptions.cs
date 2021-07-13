using System;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 域名
    /// </summary>
    [Options("Lookup")]
    public class GithubLookupFactoryOptions
    {
        /// <summary>
        /// 反查的域名
        /// </summary>
        public string[] Domains { get; set; } = Array.Empty<string>();
    }
}
