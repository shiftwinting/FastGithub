using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 表示所有中间件执行委托
    /// </summary> 
    /// <param name="context">中间件上下文</param>
    /// <returns></returns>
    delegate Task GithubScanDelegate(GithubContext context);
}
