using FastGithub.Configuration;
using FastGithub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FastGithub.Controllers
{
    /// <summary>
    /// 首页控制器
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// 首页
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var model = new Home
            {
                Version = ProductionVersion.Current?.ToString(),
                ProjectUri = "https://github.com/dotnetcore/FastGithub"
            };
            return View(model);
        }
    }
}
