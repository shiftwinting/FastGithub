using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
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
        private const string DownloadPage = "https://gitee.com/jiulang/fast-github/releases";
        private const string ReleasesUri = "https://gitee.com/api/v5/repos/jiulang/fast-github/releases?page=1&per_page=1&direction=desc";

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
                this.logger.LogInformation($"您正在使用{currentVersion}版本{Environment.NewLine}请前往{DownloadPage}下载新版本");
                this.logger.LogInformation(lastRelease.ToString());
            }
        }

        /// <summary>
        /// 获取最新发布
        /// </summary>
        /// <returns></returns>
        public async Task<Release?> GetLastedReleaseAsync(CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            var releases = await httpClient.GetFromJsonAsync<Release[]>(ReleasesUri, cancellationToken);
            return releases?.FirstOrDefault();
        }
    }
}
