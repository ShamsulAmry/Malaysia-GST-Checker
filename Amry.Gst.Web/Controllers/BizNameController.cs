using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace Amry.Gst.Web.Controllers
{
    public class BizNameController : ApiController
    {
        readonly IGstDataSource _gst = new GstWebScraper();

        public Task<IList<GstLookupResult>> Get(string id)
        {
            return _gst.LookupGstData(GstLookupInputType.BusinessName, id.Replace('_', ' '));
        }
    }
}