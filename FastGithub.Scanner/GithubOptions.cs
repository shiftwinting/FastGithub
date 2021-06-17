using System;

namespace FastGithub.Scanner
{
    [Options("Github")]
    sealed class GithubOptions
    {
        public ScanSetting Scan { get; set; } = new ScanSetting();

        public DomainAddressProviderSetting DominAddressProvider { get; set; } = new DomainAddressProviderSetting();

        public class ScanSetting
        {
            public TimeSpan FullScanInterval = TimeSpan.FromHours(2d);

            public TimeSpan ResultScanInterval = TimeSpan.FromMinutes(1d);

            public TimeSpan TcpScanTimeout { get; set; } = TimeSpan.FromSeconds(1d);

            public TimeSpan HttpsScanTimeout { get; set; } = TimeSpan.FromSeconds(2d);
        }

        public class DomainAddressProviderSetting
        {
            public DnsDomainAddressSetting DnsDomainAddress { get; set; } = new DnsDomainAddressSetting();
            public MetaDoaminAddressSetting MetaDomainAddress { get; set; } = new MetaDoaminAddressSetting();
            public IPAddressComDomainAddressSetting IPAddressComDomainAddress { get; set; } = new IPAddressComDomainAddressSetting();

            public class DnsDomainAddressSetting
            {
                public bool Enable { get; set; } = true;

                public string[] Dnss { get; set; } = Array.Empty<string>();

                public string[] Domains { get; set; } = Array.Empty<string>();
            }

            public class MetaDoaminAddressSetting
            {
                public bool Enable { get; set; } = true;

                public Uri MetaUri { get; set; } = new Uri("https://gitee.com/jiulang/fast-github/raw/master/FastGithub/meta.json");
            }

            public class IPAddressComDomainAddressSetting
            {
                public bool Enable { get; set; } = true;

                public string[] Domains { get; set; } = Array.Empty<string>();
            }
        }
    }
}
