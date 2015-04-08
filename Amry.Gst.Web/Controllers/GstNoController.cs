using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace Amry.Gst.Web.Controllers
{
    public class GstNoController : ApiController
    {
        readonly IGstDataSource _gst = new GstWebScraper();

        public async Task<GstLookupResult> Get(string id)
        {
            var result = await _gst.LookupGstData(GstLookupInputType.GstNumber, id);
            return result.FirstOrDefault();
        }
    }
}