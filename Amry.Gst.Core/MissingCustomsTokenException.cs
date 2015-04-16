using System;
using System.Collections.Generic;
using Amry.Gst.Properties;
using RestSharp;

namespace Amry.Gst
{
    public class MissingCustomsTokenException : Exception
    {
        public MissingCustomsTokenException(DateTime sessionStartTime, int requestCountBeforeError, IRestResponse response)
            : base(Resources.NoTokenReturnedErrorMessage)
        {
            SessionStartTime = sessionStartTime;
            RequestCountBeforeError = requestCountBeforeError;
            ResponseHeaders = response.Headers;
            ResponseContent = response.Content;
        }

        public DateTime SessionStartTime { get; private set; }
        public int RequestCountBeforeError { get; private set; }
        public IList<Parameter> ResponseHeaders { get; private set; }
        public string ResponseContent { get; private set; }
    }
}