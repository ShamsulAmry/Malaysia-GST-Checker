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

        public async Task<IList<IGstLookupResult>> LookupGstDataAsync(GstLookupInputType inputType, string input, bool validateInput = false)
        {
            if (validateInput) {
                GstInputValidator.ValidateInput(inputType, input);
            }

            GstWebScraper scraper;
            var pool = _pools[inputType];
            while (pool.TryDequeue(out scraper) && scraper.ShouldDispose) { }
            if (scraper == null) {
                scraper = new GstWebScraper();
            }

            try {
                return await scraper.LookupGstDataAsync(inputType, input);
            } finally {
                if (!scraper.ShouldDispose) {
                    pool.Enqueue(scraper);
                }
            }
        }
    }
}