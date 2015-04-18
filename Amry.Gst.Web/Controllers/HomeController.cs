using System.Web.Mvc;
using Amry.Gst.Properties;
using WebMarkupMin.Mvc.ActionFilters;

namespace Amry.Gst.Web.Controllers
{
    public class HomeController : Controller
    {
        [Route, MinifyHtml]
        public ActionResult Index()
        {
            return View();
        }

        [Route("about"), MinifyHtml]
        public ActionResult About()
        {
            return View();
        }

        [Route("api"), MinifyHtml]
        public ActionResult Api()
        {
            return View();
        }

        [Route("ver")]
        public ActionResult Version()
        {
            return Content(AssemblyInfoConstants.Version, "text/plain");
        }
    }
}