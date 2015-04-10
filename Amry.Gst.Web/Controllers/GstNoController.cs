using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace Amry.Gst.Web.Controllers
{
    public class GstNoController : ApiController
    {
        readonly IGstDataSource _gstDataSource;

        public GstNoController(IGstDataSource gstDataSource)
        {
            _gstDataSource = gstDataSource;
        }

        public Task<IList<IGstLookupResult>> Get(string id)
        {
            return _gstDataSource.LookupGstData(GstLookupInputType.GstNumber, id);
        }
    }
}