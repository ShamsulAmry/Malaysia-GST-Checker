using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Amry.Gst.Web.Properties;

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

            var cachedResult = results[0] as CachedGstEntity;
            if (cachedResult != null && cachedResult.KnownErrorCode != null) {
                if (cachedResult.KnownErrorCode == KnownCustomsGstErrorCode.NoResult.ToString()) {
                    return new HttpResponseMessage(HttpStatusCode.NonAuthoritativeInformation) {
                        Content = new ObjectContent<IList<IGstLookupResult>>(new IGstLookupResult[0], new JsonMediaTypeFormatter())
                    }.WithCacheTimestamp(cachedResult);
                }

                if (cachedResult.KnownErrorCode == KnownCustomsGstErrorCode.Over100Results.ToString()) {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                        ReasonPhrase = Resources.CustomsGstExceptionHttpResponseReasonPhrase,
                        Content = new StringContent(Resources.Over100Results)
                    }.WithCacheTimestamp(cachedResult);
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NonAuthoritativeInformation) {
                Content = new ObjectContent<IList<IGstLookupResult>>(results, new JsonMediaTypeFormatter())
            }.WithCacheTimestamp(cachedResult);
        }
    }

    static class HttpResponseMessageExtension
    {
        public static HttpResponseMessage WithCacheTimestamp(this HttpResponseMessage responseMsg, CachedGstEntity cachedResult)
        {
            responseMsg.Headers.Add("X-CacheTimestamp", cachedResult.Timestamp.DateTime.ToUniversalTime().ToString(CultureInfo.InvariantCulture));
            return responseMsg;
        }
    }
}