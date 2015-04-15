using System.Net;
using System.Net.Http;
using System.Web.Http;
using Amry.Gst.Web.Properties;

namespace Amry.Gst.Web.Controllers
{
    public abstract class GstApiController : ApiController
    {
        public IHttpActionResult Get()
        {
            return ResponseMessage(new HttpResponseMessage(HttpStatusCode.Forbidden) {
                ReasonPhrase = Resources.WebApiInvalidInputReasonPhrase,
                Content = new StringContent(Resources.WebApiSearchTextIsMandatory)
            });
        }
    }
}