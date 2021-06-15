using System;

namespace FastGithub.Scanner
{
    [Options("Github")]
    class GithubOptions
    {
        public TimeSpan ScanAllInterval { get; set; } = TimeSpan.FromHours(12d);

        public TimeSpan ScanResultInterval { get; set; } = TimeSpan.FromMinutes(5d);

        public Uri MetaUri { get; set; } = new Uri("https://gitee.com/jiulang/fast-github/raw/master/FastGithub/meta.json");

        public TimeSpan PortScanTimeout { get; set; } = TimeSpan.FromSeconds(1d);

        public TimeSpan HttpsScanTimeout { get; set; } = TimeSpan.FromSeconds(5d);
    }
}
