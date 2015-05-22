using System;
using System.Runtime.Serialization;

namespace Amry.Gst
{
    public interface IGstLookupResult
    {
        string LegalName { get; }
        string TradingName { get; }
        string CommenceDate { get; }
        string GstNumber { get; }
        string Status { get; }
        bool IsLiveData { get; }
    }

    [DataContract]
    public class GstLookupResult : IGstLookupResult
    {
        public GstLookupResult(string gstNumber, string legalName, string tradingName, DateTime commenceDate, string status)
        {
            GstNumber = gstNumber;
            LegalName = legalName;
            TradingName = tradingName;
            CommenceDate = commenceDate.ToString("yyyy-MM-dd");
            Status = status;
            IsLiveData = true;
        }

        [DataMember]
        public string GstNumber { get; private set; }

        [DataMember]
        public string LegalName { get; private set; }

        [DataMember]
        public string TradingName { get; private set; }

        [DataMember]
        public string CommenceDate { get; private set; }

        [DataMember]
        public string Status { get; private set; }

        public bool IsLiveData { get; set; }
    }
}