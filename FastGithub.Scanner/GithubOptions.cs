using System;

namespace FastGithub.Scanner
{
    /// <summary>
    /// github选项
    /// </summary>
    [Options("Github")]
    sealed class GithubOptions
    {
        /// <summary>
        /// 扫描配置
        /// </summary>
        public ScanSetting Scan { get; set; } = new ScanSetting();

        /// <summary>
        /// 域名与ip关系提供者配置
        /// </summary>
        public DomainAddressProvidersSetting DominAddressProviders { get; set; } = new DomainAddressProvidersSetting();

        /// <summary>
        /// 扫描配置
        /// </summary>
        public class ScanSetting
        {
            public TimeSpan FullScanInterval = TimeSpan.FromHours(2d);

            public TimeSpan ResultScanInterval = TimeSpan.FromMinutes(1d);

            public TimeSpan TcpScanTimeout { get; set; } = TimeSpan.FromSeconds(1d);

            public TimeSpan HttpsScanTimeout { get; set; } = TimeSpan.FromSeconds(5d);
        }

        /// <summary>
        /// 域名与ip关系提供者配置
        /// </summary>
        public class DomainAddressProvidersSetting
        {
            public GithubMetaProviderSetting GithubMetaProvider { get; set; } = new GithubMetaProviderSetting();
            public IPAddressComProviderSetting IPAddressComProvider { get; set; } = new IPAddressComProviderSetting();
            public PublicDnsProviderSetting PublicDnsProvider { get; set; } = new PublicDnsProviderSetting();

            public class GithubMetaProviderSetting
            {
                public bool Enable { get; set; } = true;

                public Uri MetaUri { get; set; } = new Uri("https://gitee.com/jiulang/fast-github/raw/master/FastGithub/meta.json");
            }

            public class IPAddressComProviderSetting
            {
                public bool Enable { get; set; } = true;

                public string[] Domains { get; set; } = Array.Empty<string>();
            }
            public class PublicDnsProviderSetting
            {
                public bool Enable { get; set; } = true;

                public string[] Dnss { get; set; } = Array.Empty<string>();

                public string[] Domains { get; set; } = Array.Empty<string>();
            }
        }
    }
}
