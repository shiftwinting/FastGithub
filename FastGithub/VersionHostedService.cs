using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
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
                var version = Assembly.GetEntryAssembly()?.GetName().Version;
                if (version == null)
                {
                    return;
                }

                using var httpClient = new HttpClient();
                var uri = "https://gitee.com/api/v5/repos/jiulang/fast-github/releases?page=1&per_page=1&direction=desc";
                var results = await httpClient.GetFromJsonAsync<Release[]>(uri, cancellationToken);
                var release = results?.FirstOrDefault();
                if (release == null)
                {
                    return;
                }

                if (string.Equals(release.tag_name, version.ToString(), StringComparison.OrdinalIgnoreCase) == false)
                {
                    this.logger.LogInformation($"您正在使用{version}版本{Environment.NewLine}请前往https://gitee.com/jiulang/fast-github/releases下载新版本");
                    this.logger.LogInformation(release.ToString());
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"检测升级信息失败：{ex.Message}");
            }
        }


        /// <summary>
        /// 发行记录
        /// </summary>
        private class Release
        {
            public string tag_name { get; set; } = string.Empty;

            public string body { get; set; } = string.Empty;

            public DateTime created_at { get; set; }

            public override string ToString()
            {
                return new StringBuilder()
                    .Append("最新版本：").AppendLine(this.tag_name)
                    .Append("发布时间：").AppendLine(this.created_at.ToString())
                    .AppendLine("更新内容：").AppendLine(this.body)
                    .ToString();
            }
        }
    }
}
