using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

namespace FastGithub.Upgrade
{
    /// <summary>
    /// 发行记录
    /// </summary>
    sealed class GithubRelease
    {
        /// <summary>
        /// 标签名
        /// </summary>
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        /// <summary>
        /// 是否预览版本
        /// </summary>
        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        /// <summary>
        /// 发行说明
        /// </summary>
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// 发行时间
        /// </summary>

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 下载页面
        /// </summary>
        [AllowNull]
        [JsonPropertyName("html_url")]
        public Uri HtmlUrl { get; set; }


        /// <summary>
        /// 获取产品版本
        /// </summary>
        /// <returns></returns>
        public ProductionVersion GetProductionVersion()
        {
            return ProductionVersion.Parse(this.TagName);
        }

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
