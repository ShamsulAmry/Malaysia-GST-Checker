using System;

namespace Amry.Gst
{
    public interface IGstLookupResult
    {
        string BusinessName { get; }
        string CommenceDate { get; }
        string GstNumber { get; }
        string Status { get; }
    }

    public class GstLookupResult : IGstLookupResult
    {
        public GstLookupResult(string gstNumber, string businessName, DateTime commenceDate, string status)
        {
            GstNumber = gstNumber;
            BusinessName = businessName;
            CommenceDate = commenceDate.ToString("yyyy-MM-dd");
            Status = status;
        }

        public string BusinessName { get; private set; }

        public string CommenceDate { get; private set; }

        public string GstNumber { get; private set; }

        public string Status { get; private set; }
    }
}