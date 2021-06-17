using System;



namespace FastGithub.Scanner
{
    [Options("Github")]
    sealed class GithubOptions
    {
        public ScanSetting Scan { get; set; } = new ScanSetting();

        public MetaDoaminAddressSetting MetaDomainAddress { get; set; } = new MetaDoaminAddressSetting();

        public DnsDomainAddressSetting DnsDomainAddress { get; set; } = new DnsDomainAddressSetting();

        public class ScanSetting
        {
            public TimeSpan FullScanInterval = TimeSpan.FromHours(2d);

            public TimeSpan ResultScanInterval = TimeSpan.FromMinutes(1d);

            public TimeSpan TcpScanTimeout { get; set; } = TimeSpan.FromSeconds(1d);

            public TimeSpan HttpsScanTimeout { get; set; } = TimeSpan.FromSeconds(2d);
        }


        public class MetaDoaminAddressSetting
        {
            public bool Enable { get; set; } = true;

            public Uri MetaUri { get; set; } = new Uri("https://gitee.com/jiulang/fast-github/raw/master/FastGithub/meta.json");
        }

        public class DnsDomainAddressSetting
        {
            public bool Enable { get; set; } = true;

            public string[] Dnss { get; set; } = Array.Empty<string>();

            public string[] Domains { get; set; } = Array.Empty<string>();
        }
    }
}
