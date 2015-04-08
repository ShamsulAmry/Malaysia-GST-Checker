using System.Linq;
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

        public async Task<GstLookupResult> Get(string id)
        {
            var result = await _gstDataSource.LookupGstData(GstLookupInputType.GstNumber, id);
            return result.FirstOrDefault();
        }
    }
}