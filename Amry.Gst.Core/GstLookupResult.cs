using System;

namespace Amry.Gst
{
    public class GstLookupResult
    {
        public GstLookupResult(string gstNumber, string businessName, DateTime commenceDate, string status)
        {
            GstNumber = gstNumber;
            BusinessName = businessName;
            CommenceDate = commenceDate;
            Status = status;
        }

        public string GstNumber { get; private set; }
        public string BusinessName { get; private set; }
        public DateTime CommenceDate { get; private set; }
        public string Status { get; private set; }
    }
}