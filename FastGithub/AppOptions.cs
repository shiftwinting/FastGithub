namespace FastGithub
{
    /// <summary>
    /// app选项
    /// </summary>
    public record AppOptions
    {
        /// <summary>
        /// 父进程id
        /// </summary>
        public int ParentProcessId { get; init; }

        /// <summary>
        /// udp日志服务器端口
        /// </summary>
        public int UdpLoggerPort { get; init; }
    }
}
