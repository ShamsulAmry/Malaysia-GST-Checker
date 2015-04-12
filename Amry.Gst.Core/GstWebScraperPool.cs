using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Amry.Gst
{
    public class GstWebScraperPool : IGstDataSource
    {
        readonly Dictionary<GstLookupInputType, ConcurrentQueue<GstWebScraper>> _pools;

        public GstWebScraperPool()
        {
            _pools = new Dictionary<GstLookupInputType, ConcurrentQueue<GstWebScraper>> {
                {GstLookupInputType.GstNumber, new ConcurrentQueue<GstWebScraper>()},
                {GstLookupInputType.BusinessRegNumber, new ConcurrentQueue<GstWebScraper>()},
                {GstLookupInputType.BusinessName, new ConcurrentQueue<GstWebScraper>()}
            };
        }

        public async Task<IList<IGstLookupResult>> LookupGstDataAsync(GstLookupInputType inputType, string input)
        {
            GstInputValidator.ValidateInput(inputType, input);

            var pool = _pools[inputType];

            GstWebScraper scraper;
            if (!pool.TryDequeue(out scraper)) {
                scraper = new GstWebScraper();
            }

            try {
                return await scraper.LookupGstDataAsync(inputType, input);
            } finally {
                pool.Enqueue(scraper);
            }
        }
    }
}