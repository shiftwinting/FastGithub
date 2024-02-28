namespace FastGithub.HttpServer.TcpMiddlewares
{
    /// <summary>
    /// 代理协议
    /// </summary>
    enum ProxyProtocol
    {
        /// <summary>
        /// 无代理
        /// </summary>
        None,

        /// <summary>
        /// http代理
        /// </summary>
        HttpProxy,

        /// <summary>
        /// 隧道代理
        /// </summary>
        TunnelProxy
    }
}
