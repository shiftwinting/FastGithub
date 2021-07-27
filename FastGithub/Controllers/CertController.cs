using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FastGithub.Controllers
{
    /// <summary>
    /// 证书控制器
    /// </summary>
    public class CertController : Controller
    {
        /// <summary>
        /// 下载CA证书
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var certFile = $"CACert/{nameof(FastGithub)}.cer";
            this.Response.ContentType = "application/x-x509-ca-cert";
            this.Response.Headers.Add("Content-Disposition", $"attachment;filename={nameof(FastGithub)}.cer");
            await this.Response.SendFileAsync(certFile);
            return new EmptyResult();
        }
    }
}
