using System.Web.Mvc;
using Amry.Gst.Properties;

namespace Amry.Gst.Web.Controllers
{
    public class HomeController : Controller
    {
        [Route]
        public ActionResult Index()
        {
            return View();
        }

        [Route("about")]
        public ActionResult About()
        {
            return View();
        }

        [Route("api")]
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