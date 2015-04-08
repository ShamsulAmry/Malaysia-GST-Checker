using System;

namespace Amry.Gst
{
    [Serializable]
    public class GstLookupResult
    {
        public readonly string BusinessName;
        public readonly string CommenceDate;
        public readonly string GstNumber;
        public readonly string Status;

        public GstLookupResult(string gstNumber, string businessName, DateTime commenceDate, string status)
        {
            GstNumber = gstNumber;
            BusinessName = businessName;
            CommenceDate = commenceDate.ToString("yyyy-MM-dd");
            Status = status;
        }
    }
}