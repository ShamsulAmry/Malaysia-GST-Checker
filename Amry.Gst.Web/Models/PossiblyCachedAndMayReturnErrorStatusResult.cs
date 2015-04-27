using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Amry.Gst.Web.Properties;

namespace Amry.Gst.Web.Models
{
    class PossiblyCachedAndMayReturnErrorStatusResult : IHttpActionResult
    {
        readonly Task<IList<IGstLookupResult>> _resultsTask;

        public PossiblyCachedAndMayReturnErrorStatusResult(Task<IList<IGstLookupResult>> resultsTask)
        {
            _resultsTask = resultsTask;
        }

        public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            // Check for search error status thrown as exceptions.
            IList<IGstLookupResult> results;
            try {
                results = await _resultsTask;
            } catch (CustomsGstException ex) {
                switch (ex.KnownErrorCode) {
                    case KnownCustomsGstErrorCode.Over100Results:
                        return new HttpResponseMessage(HttpStatusCode.Forbidden) {
                            ReasonPhrase = Resources.WebApiCustomsGstExceptionReasonPhrase,
                            Content = new StringContent(Resources.WebApiOver100Results)
                        };

                    case KnownCustomsGstErrorCode.StatusCode400:
                        return new HttpResponseMessage(HttpStatusCode.BadGateway) {
                            ReasonPhrase = Resources.WebApiCustomsGstExceptionReasonPhrase,
                            Content = new StringContent(ex.Message)
                        };

                    case KnownCustomsGstErrorCode.ScheduledMaintenance:
                        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) {
                            ReasonPhrase = Resources.WebApiCustomsGstExceptionReasonPhrase,
                            Content = new StringContent(ex.Message)
                        };
                }

                throw;
            } catch (InvalidGstInputException ex) {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) {
                    ReasonPhrase = Resources.WebApiInvalidInputReasonPhrase,
                    Content = new StringContent(ex.Message)
                };
            }

            // Return live data.
            if (results.Count == 0 || results[0].IsLiveData) {
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new ObjectContent<IList<IGstLookupResult>>(results, new JsonMediaTypeFormatter())
                };
            }

            // Return cached data or search error status.
            var cachedResult = results[0] as CachedGstEntity;
            if (cachedResult == null || cachedResult.KnownErrorCode == null) {
                // If not IsLiveData but it's not a CachedGstEntity,
                // or if it's a CachedGstEntity but with no KnownErrorCode,
                return new HttpResponseMessage(HttpStatusCode.NonAuthoritativeInformation) {
                    Content = new ObjectContent<IList<IGstLookupResult>>(results, new JsonMediaTypeFormatter())
                }.WithCacheTimestamp(cachedResult);
            }
            if (cachedResult.KnownErrorCode == KnownCustomsGstErrorCode.NoResult.ToString()) {
                return new HttpResponseMessage(HttpStatusCode.NonAuthoritativeInformation) {
                    Content = new ObjectContent<IList<IGstLookupResult>>(new IGstLookupResult[0], new JsonMediaTypeFormatter())
                }.WithCacheTimestamp(cachedResult);
            }
            if (cachedResult.KnownErrorCode == KnownCustomsGstErrorCode.Over100Results.ToString()) {
                return new HttpResponseMessage(HttpStatusCode.Forbidden) {
                    ReasonPhrase = Resources.WebApiCustomsGstExceptionReasonPhrase,
                    Content = new StringContent(Resources.WebApiOver100Results)
                }.WithCacheTimestamp(cachedResult);
            }

            throw new InvalidGstResultsException(results);
        }
    }

    class InvalidGstResultsException : Exception
    {
        public InvalidGstResultsException(IList<IGstLookupResult> results)
        {
            Results = results;
        }

        public IList<IGstLookupResult> Results { get; private set; }
    }

    static class HttpResponseMessageExtension
    {
        public static HttpResponseMessage WithCacheTimestamp(this HttpResponseMessage responseMsg, CachedGstEntity cachedResult)
        {
            responseMsg.Headers.Add("X-CacheDate", cachedResult.Timestamp.DateTime.ToUniversalTime().ToString("R"));
            return responseMsg;
        }
    }
}