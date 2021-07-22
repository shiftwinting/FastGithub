using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private readonly ILogger<UpgradeService> logger;
        private const string ReleasesUri = "https://api.github.com/repos/xljiulang/fastgithub/releases";

        /// <summary>
        /// 升级服务
        /// </summary>
        /// <param name="logger"></param>
        public UpgradeService(ILogger<UpgradeService> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 进行升级
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UpgradeAsync(CancellationToken cancellationToken)
        {
            var currentVersion = ProductionVersion.GetApplicationVersion();
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
                this.logger.LogInformation($"您正在使用{currentVersion}版本{Environment.NewLine}请前往{lastRelease.HtmlUrl}下载新版本");
                this.logger.LogInformation(lastRelease.ToString());
            }
        }

        /// <summary>
        /// 获取最新发布
        /// </summary>
        /// <returns></returns>
        public async Task<GithubRelease?> GetLastedReleaseAsync(CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient(new ReverseProxyHttpHandler())
            {
                Timeout = TimeSpan.FromSeconds(30d),
            };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(nameof(FastGithub), "1.0"));

            var releases = await httpClient.GetFromJsonAsync<GithubRelease[]>(ReleasesUri, cancellationToken);
            return releases?.FirstOrDefault(item => item.Prerelease == false);
        }
    }
}
