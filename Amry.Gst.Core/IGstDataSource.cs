using System.Collections.Generic;
using System.Threading.Tasks;

namespace Amry.Gst
{
    public interface IGstDataSource
    {
        Task<IList<IGstLookupResult>> LookupGstDataAsync(GstLookupInputType inputType, string input);
    }
}