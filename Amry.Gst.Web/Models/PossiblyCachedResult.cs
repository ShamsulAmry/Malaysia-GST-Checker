using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Amry.Gst.Web.Models
{
    class PossiblyCachedResult : IHttpActionResult
    {
        readonly Task<IList<IGstLookupResult>> _resultsTask;

        public PossiblyCachedResult(Task<IList<IGstLookupResult>> resultsTask)
        {
            _resultsTask = resultsTask;
        }

        public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var results = await _resultsTask;
            if (results.Count == 0 || results[0].IsLiveData) {
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new ObjectContent<IList<IGstLookupResult>>(results, new JsonMediaTypeFormatter())
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NonAuthoritativeInformation) {
                Content = new ObjectContent<IList<IGstLookupResult>>(results, new JsonMediaTypeFormatter())
            };
        }
    }
}