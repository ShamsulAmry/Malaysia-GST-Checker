using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace Amry.Gst
{
    public class GstWebScraper : IGstDataSource, IDisposable
    {
        readonly HttpClient _client = new HttpClient() {
            BaseAddress = new Uri("https://gst.customs.gov.my/TAP/")
        };

        GstLookupInputType? _inputType;
        bool _isInitialized;
        string _token;

        public void Dispose()
        {
            _client.Dispose();
        }

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
            var resp = await _client.GetAsync("GetWlbToken", HttpCompletionOption.ResponseHeadersRead);
            UpdateTokenForNextRequest(resp);
        }

        async Task LoadFrontPage()
        {
            var resp = await _client.GetAsync("_/?Load=1&FAST_VERLAST__" + _token, HttpCompletionOption.ResponseHeadersRead);
            UpdateTokenForNextRequest(resp);
        }

        async Task BrowseToLookupPage()
        {
            var formData = new Dictionary<string, string> {
                {"DOC_MODAL_ID__", "0"},
                {"EVENT__", "b-i"},
                {"FAST_VERLAST__", _token}
            };
            var resp = await _client.PostAsync("_/EventOccurred", new FormUrlEncodedContent(formData));
            UpdateTokenForNextRequest(resp);
        }

        async Task SelectLookupInputType(GstLookupInputType inputType)
        {
            var formData = new Dictionary<string, string> {
                {"DOC_MODAL_ID__", "0"},
                {"FAST_VERLAST__", _token}
            };

            switch (inputType) {
                case GstLookupInputType.GstNumber:
                    formData.Add("d-3", "true");
                    break;

                case GstLookupInputType.BusinessRegNumber:
                    formData.Add("d-6", "true");
                    break;

                case GstLookupInputType.BusinessName:
                    formData.Add("d-8", "true");
                    break;
            }

            var resp = await _client.PostAsync("_/Recalc", new FormUrlEncodedContent(formData));
            UpdateTokenForNextRequest(resp);
            _inputType = inputType;
        }

        async Task<IList<GstLookupResult>> ExecuteLookup(string input)
        {
            var formData = new Dictionary<string, string> {
                {"DOC_MODAL_ID__", "0"},
                {"FAST_VERLAST__", _token}
            };

            switch (_inputType) {
                case GstLookupInputType.GstNumber:
                    formData.Add("d-5", input);
                    break;

                case GstLookupInputType.BusinessRegNumber:
                    formData.Add("d-7", input);
                    break;

                case GstLookupInputType.BusinessName:
                    formData.Add("d-9", input);
                    break;
            }

            var resp = await _client.PostAsync("_/Recalc", new FormUrlEncodedContent(formData));
            UpdateTokenForNextRequest(resp);

            var respContent = await resp.Content.ReadAsStringAsync();
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

        void UpdateTokenForNextRequest(HttpResponseMessage response)
        {
            _token = response.Headers.GetValues("Fast-Ver-Last").First();
        }
    }
}