using FastGithub.Models;
using Microsoft.AspNetCore.Mvc;

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
            var model = new Home { Host = Request.Host.ToString() };
            return View(model);
        } 
    }
}
