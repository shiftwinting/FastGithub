namespace FastGithub.Configuration
{
    /// <summary>
    /// 监听选项
    /// </summary>
    public class FastGithubListenOptions
    {
        /// <summary>
        /// 监听的ssh端口
        /// </summary>
        public int SshPort { get; set; } = 22;

        /// <summary>
        /// 监听的dns端口
        /// </summary>
        public int DnsPort { get; set; } = 53;
    }
}
