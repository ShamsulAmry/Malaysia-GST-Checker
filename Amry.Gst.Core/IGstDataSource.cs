using System.Collections.Generic;
using System.Threading.Tasks;

namespace Amry.Gst
{
    public interface IGstDataSource
    {
        Task<IList<GstLookupResult>> LookupGstData(GstLookupInputType inputType, string input);
    }
}