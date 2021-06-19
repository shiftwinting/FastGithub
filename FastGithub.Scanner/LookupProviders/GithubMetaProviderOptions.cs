using System;

namespace FastGithub.Scanner.LookupProviders
{
    [Options("Github:Lookup:GithubMetaProvider")]
    sealed class GithubMetaProviderOptions
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// meta请求uri
        /// </summary>
        public Uri MetaUri { get; set; } = new Uri("https://gitee.com/jiulang/fast-github/raw/master/FastGithub/meta.json");
    }
}
