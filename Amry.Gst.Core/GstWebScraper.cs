using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amry.Gst.Properties;
using HtmlAgilityPack;
using Newtonsoft.Json;
using RestSharp;

namespace Amry.Gst
{
    public class GstWebScraper : IGstDataSource
    {
        static readonly Uri CustomsEndpoint = new Uri("https://gst.customs.gov.my/TAP/");

        readonly DateTime _startTime = DateTime.Now;
        readonly RestClient _client = new RestClient(CustomsEndpoint) {
            CookieContainer = new CookieContainer()
        };
        readonly StringBuilder _requestLogBuilder = new StringBuilder();

        DateTime _lastRequestTime = DateTime.Now;
        int _requestCount;
        bool _forceShouldDispose;
        Tuple<GstLookupInputType, string> _previousInput;
        IList<IGstLookupResult> _previousResults;
        int _accessCount;
        GstLookupInputType? _inputType;
        bool _isInitialized;
        string _token;
        bool _daShowResults;
        bool _diNoRegistrantsFound;
        bool _dlOver100Results;

        static GstWebScraper()
        {
            var customsServicePoint = ServicePointManager.FindServicePoint(CustomsEndpoint);
            customsServicePoint.UseNagleAlgorithm = false;
            customsServicePoint.Expect100Continue = false;
            customsServicePoint.ConnectionLimit = 30;
        }

        public bool ShouldDispose
        {
            get { return _forceShouldDispose || _requestCount >= 100 || (DateTime.Now - _lastRequestTime).TotalMinutes >= 15; }
        }

        public async Task<IList<IGstLookupResult>> LookupGstDataAsync(GstLookupInputType inputType, string input, bool validateInput = false)
        {
            // Customs' server will return empty result if I send the same request two times in a row, 
            // so I'm going to cache the most recent result and return that if the same requests were sent twice.
            var currentInput = Tuple.Create(inputType, input);
            if (currentInput.Equals(_previousInput)) {
                if (_previousResults.Count > 0) {
                    var result = (GstLookupResult) _previousResults[0];
                    result.IsLiveData = false;
                }
                return _previousResults;
            }

            if (validateInput) {
                GstInputValidator.ValidateInput(inputType, input);
            }

            if (_accessCount > 0) {
                throw new NotSupportedException(Resources.SingleLookupErrorMessage);
            }

            try {
                if (Interlocked.Increment(ref _accessCount) > 1) {
                    throw new NotSupportedException(Resources.SingleLookupErrorMessage);
                }

                if (!_isInitialized) {
                    await InitializeTokenAsync();
                    await LoadFrontPageAsync();
                    await BrowseToLookupPageAsync();
                    _isInitialized = true;
                }

                if (_inputType == null || _inputType != inputType) {
                    await SelectLookupInputTypeAsync(inputType);
                }

                var results = await ExecuteLookupAsync(input);
                _previousInput = currentInput;
                _previousResults = results;
                return results;
            } catch (WebException ex) {
                throw new CustomsGstException(ex.Message, innerException: ex);
            } finally {
                Interlocked.Decrement(ref _accessCount);
            }
        }

        async Task InitializeTokenAsync()
        {
            var req = new RestRequest("GetWlbToken");
            var resp = await _client.ExecuteGetTaskAsync(req);
            UpdateTokenForNextRequest(resp);
        }

        async Task LoadFrontPageAsync()
        {
            var req = new RestRequest("_/");
            req.AddParameter("Load", 1);
            req.AddParameter("FAST_VERLAST__", _token);
            var resp = await _client.ExecuteGetTaskAsync(req);
            UpdateTokenForNextRequest(resp);
        }

        async Task BrowseToLookupPageAsync()
        {
            var req = new RestRequest("_/EventOccurred");
            req.AddParameter("DOC_MODAL_ID__", 0);
            req.AddParameter("EVENT__", "b-i");
            req.AddParameter("FAST_VERLAST__", _token);
            var resp = await _client.ExecutePostTaskAsync(req);
            UpdateTokenForNextRequest(resp);
        }

        async Task SelectLookupInputTypeAsync(GstLookupInputType inputType)
        {
            var req = new RestRequest("_/Recalc");

            switch (inputType) {
                case GstLookupInputType.GstNumber:
                    req.AddParameter("d-3", "true");
                    break;

                case GstLookupInputType.BusinessRegNumber:
                    req.AddParameter("d-6", "true");
                    break;

                case GstLookupInputType.BusinessName:
                    req.AddParameter("d-8", "true");
                    break;
            }

            req.AddParameter("DOC_MODAL_ID__", 0);
            req.AddParameter("FAST_VERLAST__", _token);
            var resp = await _client.ExecutePostTaskAsync(req);
            UpdateTokenForNextRequest(resp);
            _inputType = inputType;
        }

        async Task<IList<IGstLookupResult>> ExecuteLookupAsync(string input)
        {
            var req = new RestRequest("_/Recalc");

            switch (_inputType) {
                case GstLookupInputType.GstNumber:
                    req.AddParameter("d-5", input);
                    break;

                case GstLookupInputType.BusinessRegNumber:
                    req.AddParameter("d-7", input);
                    break;

                case GstLookupInputType.BusinessName:
                    req.AddParameter("d-9", input);
                    break;
            }

            req.AddParameter("DOC_MODAL_ID__", 0);
            req.AddParameter("FAST_VERLAST__", _token);
            var resp = await _client.ExecutePostTaskAsync(req);
            UpdateTokenForNextRequest(resp);

            string respContent;
            // resp.Content can't be directly used for deserialization due to UTF-8 BOM.
            using (var reader = new StreamReader(new MemoryStream(resp.RawBytes))) {
                respContent = reader.ReadToEnd();
            }
            var jsonResult = JsonConvert.DeserializeObject<GstJsonResult>(respContent);

            // Pass on validation error message returned by the Customs' server.
            // But I've already covered the validation I already knew of without having to send invalid request.
            var errorField = jsonResult.Updates.FieldUpdates.FirstOrDefault(x => x.IndicatorClass == "FieldError");
            if (errorField != null) {
                throw new CustomsGstException(errorField.Message);
            }

            // Keep track of Customs' page state
            foreach (var update in jsonResult.Updates.FieldUpdates) {
                switch (update.Field) {
                    case "d-a":
                        _daShowResults = update.Visible ?? false;
                        break;

                    case "d-i":
                        _diNoRegistrantsFound = update.Visible ?? false;
                        break;

                    case "d-l":
                        _dlOver100Results = update.Visible ?? false;
                        break;
                }
            }

            if (_dlOver100Results) {
                throw new CustomsGstException(Resources.Over100ResultsErrorMessage, KnownCustomsGstErrorCode.Over100Results);
            }

            if (_diNoRegistrantsFound || !_daShowResults) {
                return new IGstLookupResult[0];
            }

            // If there is no previous result returned by the Customs' server 
            // and current result is also empty, they will not return any table output.
            var resultField = jsonResult.Updates.FieldUpdates.FirstOrDefault(update => update.Field == "d-f");
            if (resultField == null) {
                return new IGstLookupResult[0];
            }

            // Other than that, return the result here.
            var htmlStr = resultField.Value;
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlStr);

            var cellTexts = htmlDoc.DocumentNode
                .SelectNodes("//tbody/tr/td")
                .Select(x => x.InnerText)
                .ToArray();

            return Enumerable.Range(0, cellTexts.Length/4)
                .Select(i => i*4)
                .Select(i => new GstLookupResult(
                    cellTexts[i],
                    cellTexts[i + 1],
                    DateTime.ParseExact(cellTexts[i + 2], "dd-MMM-yyyy", CultureInfo.InvariantCulture),
                    cellTexts[i + 3]))
                .ToArray();
        }

        void UpdateTokenForNextRequest(IRestResponse response)
        {
            _requestLogBuilder.AppendFormat("{0}: {1}. ", _requestCount, DateTime.Now - _lastRequestTime);

            var tokenHeader = response.Headers.FirstOrDefault(x => x.Name == "Fast-Ver-Last");
            if (tokenHeader == null) {
                _forceShouldDispose = true;
                throw new MissingCustomsTokenException(_startTime, _lastRequestTime, 
                    _requestCount, _requestLogBuilder.ToString(), response);
            }

            _token = (string) tokenHeader.Value;
            _lastRequestTime = DateTime.Now;
            _requestCount++;
        }
    }
}