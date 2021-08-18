namespace FastGithub.Configuration
{
    /// <summary>
    /// 监听配置
    /// </summary>
    public record ListenConfig
    {
        /// <summary>
        /// 监听的ssh端口
        /// </summary>
        public int SshPort { get; init; } = 22;

        /// <summary>
        /// 监听的dns端口
        /// </summary>
        public int DnsPort { get; init; } = 53;
    }
}
