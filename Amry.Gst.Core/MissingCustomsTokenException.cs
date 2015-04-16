using System;
using System.Collections.Generic;
using System.Globalization;
using Amry.Gst.Properties;
using RestSharp;

namespace Amry.Gst
{
    public class MissingCustomsTokenException : Exception
    {
        public MissingCustomsTokenException(DateTime sessionStartTime, int requestCountBeforeError, string requestLog, IRestResponse response)
            : base(Resources.NoTokenReturnedErrorMessage)
        {
            SessionStartTime = sessionStartTime;
            RequestCountBeforeError = requestCountBeforeError;

            ResponseDetails = new Dictionary<string, string> {
                {"LiveSpan", (DateTime.Now - sessionStartTime).ToString()}, 
                {"SessionStartTime", sessionStartTime.ToString("R")}, 
                {"RequestCountBeforeError", requestCountBeforeError.ToString(CultureInfo.InvariantCulture)}, 
                {"RequestLog", requestLog}, {"Content", response.Content}
            };

            foreach (var header in response.Headers) {
                ResponseDetails.Add("Header: " + header.Name, (string) header.Value);
            }
        }

        public DateTime SessionStartTime { get; private set; }
        public int RequestCountBeforeError { get; private set; }
        public IDictionary<string, string> ResponseDetails { get; private set; }
    }
}