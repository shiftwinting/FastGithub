using System;

namespace FastGithub
{
    class GithubOptions
    {
        public Uri MetaUri { get; set; } = new Uri("https://gitee.com/jiulang/fast-github/raw/master/FastGithub/meta.json");

        public int Concurrent { get; set; } = 50;

        public TimeSpan PortScanTimeout { get; set; } = TimeSpan.FromSeconds(1d);

        public TimeSpan HttpTestTimeout { get; set; } = TimeSpan.FromSeconds(5d);
    }
}
