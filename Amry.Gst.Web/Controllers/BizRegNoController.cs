using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace Amry.Gst.Web.Controllers
{
    public class BizRegNoController : ApiController
    {
        readonly IGstDataSource _gstDataSource;

        public BizRegNoController(IGstDataSource gstDataSource)
        {
            _gstDataSource = gstDataSource;
        }

        public Task<IList<GstLookupResult>> Get(string id)
        {
            return _gstDataSource.LookupGstData(GstLookupInputType.BusinessRegNumber, id);
        }
    }
}