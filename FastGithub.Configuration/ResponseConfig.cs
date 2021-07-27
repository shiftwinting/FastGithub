namespace FastGithub.Configuration
{
    /// <summary>
    /// 响应配置
    /// </summary>
    public record ResponseConfig
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int StatusCode { get; init; } = 200;

        /// <summary>
        /// 内容类型
        /// </summary>
        public string ContentType { get; init; } = "text/plain;charset=utf-8";

        /// <summary>
        /// 内容的值
        /// </summary>
        public string? ContentValue { get; init; }
    }
}
