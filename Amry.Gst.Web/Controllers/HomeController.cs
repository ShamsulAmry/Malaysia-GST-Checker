using System.Web.Mvc;
using Amry.Gst.Properties;
using WebMarkupMin.Mvc.ActionFilters;

namespace Amry.Gst.Web.Controllers
{
    public class HomeController : Controller
    {
        const int OneWeek = 604800;
        const int OneYear = 31536000;

        [Route, MinifyHtml, OutputCache(Duration = OneWeek)]
        public ActionResult Index()
        {
            return View();
        }

        [Route("about"), MinifyHtml, OutputCache(Duration = OneYear)]
        public ActionResult About()
        {
            return View();
        }

        [Route("api"), MinifyHtml, OutputCache(Duration = OneYear)]
        public ActionResult Api()
        {
            return View();
        }

        [Route("ver")]
        public ActionResult Version()
        {
            return Content("Version: " + AssemblyInfoConstants.Version, "text/plain");
        }
    }
}