using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Upgrade
{
    /// <summary>
    /// 升级检查后台服务
    /// </summary>
    sealed class UpgradeHostedService : IHostedService
    {
        private readonly ILogger<UpgradeHostedService> logger;
        private const string DownloadPage = "https://gitee.com/jiulang/fast-github/releases";
        private const string ReleasesUri = "https://gitee.com/api/v5/repos/jiulang/fast-github/releases?page=1&per_page=1&direction=desc";

        public UpgradeHostedService(ILogger<UpgradeHostedService> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 检测版本
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var currentVersion = GetCurrentVersion();
                if (currentVersion == null)
                {
                    return;
                }

                var lastRelease = await GetLastedReleaseAsync(cancellationToken);
                if (lastRelease == null)
                {
                    return;
                }

                var lastedVersion = ProductionVersion.Parse(lastRelease.TagName);
                if (lastedVersion.CompareTo(currentVersion) > 0)
                {
                    this.logger.LogInformation($"您正在使用{currentVersion}版本{Environment.NewLine}请前往{DownloadPage}下载新版本");
                    this.logger.LogInformation(lastRelease.ToString());
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"检测升级信息失败：{ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取当前版本
        /// </summary>
        /// <returns></returns>
        private static ProductionVersion? GetCurrentVersion()
        {
            var version = Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            return version == null ? null : ProductionVersion.Parse(version);
        }

        /// <summary>
        /// 获取最新发布
        /// </summary>
        /// <returns></returns>
        private static async Task<Release?> GetLastedReleaseAsync(CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            var releases = await httpClient.GetFromJsonAsync<Release[]>(ReleasesUri, cancellationToken);
            return releases?.FirstOrDefault();
        }

        /// <summary>
        /// 发行记录
        /// </summary>
        private class Release
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = string.Empty;


            [JsonPropertyName("body")]
            public string Body { get; set; } = string.Empty;


            [JsonPropertyName("created_at")]
            public DateTime CreatedAt { get; set; }

            public override string ToString()
            {
                return new StringBuilder()
                    .Append("最新版本：").AppendLine(this.TagName)
                    .Append("发布时间：").AppendLine(this.CreatedAt.ToString())
                    .AppendLine("更新内容：").AppendLine(this.Body)
                    .ToString();
            }
        }
    }
}
