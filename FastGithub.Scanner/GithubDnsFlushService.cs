using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FastGithub.Scanner
{
    [Service(ServiceLifetime.Singleton)]
    sealed class GithubDnsFlushService
    {
        private readonly ILogger<GithubDnsFlushService> logger;
        private readonly IOptionsMonitor<GithubLookupFactoryOptions> options;

        [SupportedOSPlatform("windows")]
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCacheEntry_A", CharSet = CharSet.Ansi)]
        private static extern int DnsFlushResolverCacheEntry(string hostName);

        public GithubDnsFlushService(
            ILogger<GithubDnsFlushService> logger,
            IOptionsMonitor<GithubLookupFactoryOptions> options)
        {
            this.logger = logger;
            this.options = options;
        }

        public void FlushGithubResolverCache()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    foreach (var domain in this.options.CurrentValue.Domains)
                    {
                        DnsFlushResolverCacheEntry(domain);
                    }
                    this.logger.LogInformation($"刷新本机相关域名的dns缓存成功");
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning($"刷新本机相关域名的dns缓存失败：{ex.Message}");
                }
            }
        }
    }
}
