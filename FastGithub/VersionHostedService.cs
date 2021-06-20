using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// 版本检查
    /// </summary>
    sealed class VersionHostedService : IHostedService
    {
        private readonly ILogger<VersionHostedService> logger;
        private const string DownloadPage = "https://gitee.com/jiulang/fast-github/releases";
        private const string ReleasesUri = "https://gitee.com/api/v5/repos/jiulang/fast-github/releases?page=1&per_page=1&direction=desc";

        public VersionHostedService(ILogger<VersionHostedService> logger)
        {
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return this.CheckVersionAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 检测新版本
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task CheckVersionAsync(CancellationToken cancellationToken)
        {
            try
            {
                var version = Assembly
                    .GetEntryAssembly()?
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion;

                if (version == null)
                {
                    return;
                }

                using var httpClient = new HttpClient();
                var releases = await httpClient.GetFromJsonAsync<Release[]>(ReleasesUri, cancellationToken);
                var lastRelease = releases?.FirstOrDefault();
                if (lastRelease == null)
                {
                    return;
                }

                if (VersionCompare(lastRelease.TagName, version) > 0)
                {
                    this.logger.LogInformation($"您正在使用{version}版本{Environment.NewLine}请前往{DownloadPage}下载新版本");
                    this.logger.LogInformation(lastRelease.ToString());
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"检测升级信息失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 版本比较
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static int VersionCompare(string x, string y)
        {
            const string VERSION = @"^\d+\.(\d+.){0,2}\d+";
            var xVersion = Regex.Match(x, VERSION).Value;
            var yVersion = Regex.Match(y, VERSION).Value;

            var xSubVersion = x[xVersion.Length..];
            var ySubVersion = y[yVersion.Length..];

            var value = Version.Parse(xVersion).CompareTo(Version.Parse(yVersion));
            if (value == 0)
            {
                value = SubCompare(xSubVersion, ySubVersion);
            }
            return value;

            static int SubCompare(string subX, string subY)
            {
                if (subX.Length == 0 && subY.Length == 0)
                {
                    return 0;
                }
                if (subX.Length == 0)
                {
                    return 1;
                }
                if (subY.Length == 0)
                {
                    return -1;
                }

                return StringComparer.OrdinalIgnoreCase.Compare(subX, subY);
            }
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
