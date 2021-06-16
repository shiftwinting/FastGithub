using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// 表示所有中间件执行委托
    /// </summary>
    /// <typeparam name="TContext">中间件上下文类型</typeparam>
    /// <param name="context">中间件上下文</param>
    /// <returns></returns>
    public delegate Task InvokeDelegate<TContext>(TContext context);
}
