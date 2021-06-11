using System;

namespace FastGithub
{
    /// <summary>
    /// 定义中间件管道创建者的接口
    /// </summary> 
    interface IGithubBuilder
    {
        /// <summary>
        /// 获取服务提供者
        /// </summary>
        IServiceProvider AppServices { get; }

        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <param name="middleware">中间件</param>
        /// <returns></returns>
        IGithubBuilder Use(Func<GithubDelegate, GithubDelegate> middleware);

        /// <summary>
        /// 创建所有中间件执行处理者
        /// </summary>
        /// <returns></returns>
        GithubDelegate Build();
    }
}
