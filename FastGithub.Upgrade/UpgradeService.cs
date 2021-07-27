using FastGithub.DomainResolve;
using FastGithub.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Upgrade
{
    /// <summary>
    /// 升级服务
    /// </summary>
    sealed class UpgradeService
    {
        private readonly IDomainResolver domainResolver;
        private readonly ILogger<UpgradeService> logger;
        private readonly Uri releasesUri = new("https://api.github.com/repos/xljiulang/fastgithub/releases");

        /// <summary>
        /// 升级服务
        /// </summary>
        /// <param name="domainResolver"></param>
        /// <param name="logger"></param>
        public UpgradeService(
            IDomainResolver domainResolver,
            ILogger<UpgradeService> logger)
        {
            this.domainResolver = domainResolver;
            this.logger = logger;
        }

        /// <summary>
        /// 进行升级
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UpgradeAsync(CancellationToken cancellationToken)
        {
            var currentVersion = ProductionVersion.GetAppVersion();
            if (currentVersion == null)
            {
                return;
            }

            var lastRelease = await this.GetLastedReleaseAsync(cancellationToken);
            if (lastRelease == null)
            {
                return;
            }

            var lastedVersion = lastRelease.GetProductionVersion();
            if (lastedVersion.CompareTo(currentVersion) > 0)
            {
                this.logger.LogInformation($"当前版本{currentVersion}为老版本{Environment.NewLine}请前往{lastRelease.HtmlUrl}下载新版本");
                this.logger.LogInformation(lastRelease.ToString());
            }
            else
            {
                this.logger.LogInformation($"当前版本{currentVersion}为最新版本");
            }
        }

        /// <summary>
        /// 获取最新发布
        /// </summary>
        /// <returns></returns>
        public async Task<GithubRelease?> GetLastedReleaseAsync(CancellationToken cancellationToken)
        {
            var domainConfig = new DomainConfig
            {
                TlsSni = false,
                TlsIgnoreNameMismatch = true,
                Timeout = TimeSpan.FromSeconds(30d)
            };

            using var request = new GithubRequestMessage
            {
                RequestUri = this.releasesUri
            };

            using var httpClient = new HttpClient(domainConfig, this.domainResolver);
            var response = await httpClient.SendAsync(request, cancellationToken);
            var releases = await response.Content.ReadFromJsonAsync<GithubRelease[]>(cancellationToken: cancellationToken);
            return releases?.FirstOrDefault(item => item.Prerelease == false);
        }
    }
}
