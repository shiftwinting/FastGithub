using FastGithub.Configuration;
using FastGithub.HttpServer.TcpMiddlewares;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FastGithub.HttpServer.HttpMiddlewares
{
    /// <summary>
    /// http代理策略中间件
    /// </summary>
    sealed class HttpProxyPacMiddleware
    {
        private readonly FastGithubConfig fastGithubConfig;

        /// <summary>
        /// http代理策略中间件
        /// </summary>
        /// <param name="fastGithubConfig"></param> 
        public HttpProxyPacMiddleware(FastGithubConfig fastGithubConfig)
        {
            this.fastGithubConfig = fastGithubConfig;
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            // http请求经过了httpProxy中间件
            var proxyFeature = context.Features.Get<IHttpProxyFeature>();
            if (proxyFeature != null && proxyFeature.ProxyProtocol == ProxyProtocol.None)
            {
                var proxyPac = this.CreateProxyPac(context.Request.Host);
                context.Response.ContentType = "application/x-ns-proxy-autoconfig";
                context.Response.Headers.Add("Content-Disposition", $"attachment;filename=proxy.pac");
                await context.Response.WriteAsync(proxyPac);
            }
            else
            {
                await next(context);
            }
        }

        /// <summary>
        /// 创建proxypac脚本
        /// </summary>
        /// <param name="proxyHost"></param>
        /// <returns></returns>
        private string CreateProxyPac(HostString proxyHost)
        {
            var buidler = new StringBuilder();
            buidler.AppendLine("function FindProxyForURL(url, host){");
            buidler.AppendLine($"    var fastgithub = 'PROXY {proxyHost}';");
            foreach (var domain in fastGithubConfig.GetDomainPatterns())
            {
                buidler.AppendLine($"    if (shExpMatch(host, '{domain}')) return fastgithub;");
            }
            buidler.AppendLine("    return 'DIRECT';");
            buidler.AppendLine("}");
            return buidler.ToString();
        }
    }
}