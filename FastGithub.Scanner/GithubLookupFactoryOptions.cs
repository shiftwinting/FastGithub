using System.Collections.Generic;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 域名
    /// </summary>
    [Options("Lookup")]
    class GithubLookupFactoryOptions
    {
        /// <summary>
        /// 反查的域名
        /// </summary>
        public HashSet<string> Domains { get; set; } = new();
    }
}
