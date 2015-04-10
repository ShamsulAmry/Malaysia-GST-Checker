using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace Amry.Gst.Web.Controllers
{
    public class BizNameController : ApiController
    {
        readonly IGstDataSource _gstDataSource;

        public BizNameController(IGstDataSource gstDataSource)
        {
            _gstDataSource = gstDataSource;
        }

        public Task<IList<IGstLookupResult>> Get(string id)
        {
            return _gstDataSource.LookupGstData(GstLookupInputType.BusinessName, id.Replace('_', ' '));
        }
    }
}