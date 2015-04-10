using System.Collections.Generic;
using System.Threading.Tasks;

namespace Amry.Gst
{
    public interface IGstDataSource
    {
        Task<IList<IGstLookupResult>> LookupGstData(GstLookupInputType inputType, string input);
    }
}