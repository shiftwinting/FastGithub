using Yarp.ReverseProxy.Forwarder;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 反向代理选项
    /// </summary>
    [Options("ReverseProxy")]
    public class GithubReverseProxyOptions
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// 每个服务的最大代理连接数
        /// </summary>
        public int MaxConnectionsPerServer { get; set; } = int.MaxValue;

        /// <summary>
        /// 请求配置
        /// </summary>
        public ForwarderRequestConfig ForwarderRequestConfig { get; set; } = new();
    }
}
