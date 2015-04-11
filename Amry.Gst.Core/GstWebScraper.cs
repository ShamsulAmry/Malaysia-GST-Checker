using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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
        static readonly Regex GstNumberRegex = new Regex(@"^\d{12}$", RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);

        readonly RestClient _client = new RestClient("https://gst.customs.gov.my/TAP/") {
            CookieContainer = new CookieContainer()
        };

        int _accessCount;
        GstLookupInputType? _inputType;
        bool _isInitialized;
        string _token;

        public async Task<IList<IGstLookupResult>> LookupGstData(GstLookupInputType inputType, string input)
        {
            if (inputType == GstLookupInputType.GstNumber && !GstNumberRegex.IsMatch(input)) {
                
            }

            if (_accessCount > 0) {
                throw new NotSupportedException(Resources.SingleLookupErrorMessage);
            }

            try {
                if (Interlocked.Increment(ref _accessCount) > 1) {
                    throw new NotSupportedException(Resources.SingleLookupErrorMessage);
                }

                if (!_isInitialized) {
                    await InitializeToken();
                    await LoadFrontPage();
                    await BrowseToLookupPage();
                    _isInitialized = true;
                }

                if (_inputType == null || _inputType != inputType) {
                    await SelectLookupInputType(inputType);
                }

                var result = await ExecuteLookup(input);
                return result;
            } finally {
                Interlocked.Decrement(ref _accessCount);
            }
        }

        async Task InitializeToken()
        {
            var req = new RestRequest("GetWlbToken");
            var resp = await _client.ExecuteGetTaskAsync(req);
            UpdateTokenForNextRequest(resp);
        }

        async Task LoadFrontPage()
        {
            var req = new RestRequest("_/");
            req.AddParameter("Load", 1);
            req.AddParameter("FAST_VERLAST__", _token);
            var resp = await _client.ExecuteGetTaskAsync(req);
            UpdateTokenForNextRequest(resp);
        }

        async Task BrowseToLookupPage()
        {
            var req = new RestRequest("_/EventOccurred");
            req.AddParameter("DOC_MODAL_ID__", 0);
            req.AddParameter("EVENT__", "b-i");
            req.AddParameter("FAST_VERLAST__", _token);
            var resp = await _client.ExecutePostTaskAsync(req);
            UpdateTokenForNextRequest(resp);
        }

        async Task SelectLookupInputType(GstLookupInputType inputType)
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

        async Task<IList<IGstLookupResult>> ExecuteLookup(string input)
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

            var errorField = jsonResult.Updates.FieldUpdates.FirstOrDefault(x => x.IndicatorClass == "FieldError");
            if (errorField != null) {
                throw new CustomsGstException(errorField.Message);
            }

            var resultField = jsonResult.Updates.FieldUpdates.LastOrDefault();
            if (resultField == null || resultField.IsTable == null) {
                return new GstLookupResult[0];
            }

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
            _token = (string) response.Headers
                .FirstOrDefault(x => x.Name == "Fast-Ver-Last")
                .Value;
        }
    }
}