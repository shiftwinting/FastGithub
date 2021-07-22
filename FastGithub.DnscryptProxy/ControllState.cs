namespace FastGithub.DnscryptProxy
{
    /// <summary>
    /// 服务控制状态
    /// </summary>
    enum ControllState
    {
        /// <summary>
        /// 未控制
        /// </summary>
        None,

        /// <summary>
        /// 控制启动
        /// </summary>
        Started,

        /// <summary>
        /// 控制停止
        /// </summary>
        Stopped,
    }
}
