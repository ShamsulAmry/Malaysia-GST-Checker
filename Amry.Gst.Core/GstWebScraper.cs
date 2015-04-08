using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using RestSharp;

namespace Amry.Gst
{
    public class GstWebScraper : IGstDataSource
    {
        readonly RestClient _client = new RestClient("https://gst.customs.gov.my/TAP/") {
            CookieContainer = new CookieContainer()
        };

        GstLookupInputType? _inputType;
        bool _isInitialized;
        string _token;

        public async Task<IList<GstLookupResult>> LookupGstData(GstLookupInputType inputType, string input)
        {
            if (!_isInitialized) {
                await InitializeToken();
                await LoadFrontPage();
                await BrowseToLookupPage();
                _isInitialized = true;
            }

            if (_inputType == null || _inputType != inputType) {
                await SelectLookupInputType(inputType);
            }

            return await ExecuteLookup(input);
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

        async Task<IList<GstLookupResult>> ExecuteLookup(string input)
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

            var htmlStr = jsonResult.Updates.FieldUpdates.Last().Value;
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