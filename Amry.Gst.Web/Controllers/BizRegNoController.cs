using System.Web.Http;
using Amry.Gst.Web.Models;

namespace Amry.Gst.Web.Controllers
{
    public class BizRegNoController : ApiController
    {
        readonly IGstDataSource _gstDataSource;

        public BizRegNoController(IGstDataSource gstDataSource)
        {
            _gstDataSource = gstDataSource;
        }

        public IHttpActionResult Get(string id)
        {
            return new PossiblyCachedResult(_gstDataSource.LookupGstDataAsync(GstLookupInputType.BusinessRegNumber, id));
        }
    }
}