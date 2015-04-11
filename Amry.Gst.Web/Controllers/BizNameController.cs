using System.Web.Http;
using Amry.Gst.Web.Models;

namespace Amry.Gst.Web.Controllers
{
    public class BizNameController : ApiController
    {
        readonly IGstDataSource _gstDataSource;

        public BizNameController(IGstDataSource gstDataSource)
        {
            _gstDataSource = gstDataSource;
        }

        public IHttpActionResult Get(string id)
        {
            return new PossiblyCachedResult(_gstDataSource.LookupGstData(
                GstLookupInputType.BusinessName, id.Replace('_', ' ')));
        }
    }
}