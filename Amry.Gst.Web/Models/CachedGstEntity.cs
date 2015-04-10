using System;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.WindowsAzure.Storage.Table;

namespace Amry.Gst.Web.Models
{
    [DataContract]
    class CachedGstEntity : TableEntity, IGstLookupResult
    {
        [DataMember]
        public string GstNumber { get; set; }

        [DataMember]
        public string BusinessName { get; set; }

        [DataMember]
        public string CommenceDate { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public DateTimeOffset CacheTimestamp
        {
            get { return Timestamp.ToOffset(TimeSpan.FromHours(8)); }
        }

        public static string GetPartitionKeyForGstNumber(string gstNumber)
        {
            return "GST-" + gstNumber;
        }

        public static string GetPartitionKeyForBusinessRegNumber(string businessRegNumber)
        {
            return "REG-" + businessRegNumber.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant);
        }

        public static CachedGstEntity Create(IGstLookupResult other, string businessRegNumber = null)
        {
            return new CachedGstEntity {
                PartitionKey = businessRegNumber == null
                    ? GetPartitionKeyForGstNumber(other.GstNumber)
                    : GetPartitionKeyForBusinessRegNumber(businessRegNumber),
                RowKey = "",
                GstNumber = other.GstNumber,
                BusinessName = other.BusinessName,
                CommenceDate = other.CommenceDate,
                Status = other.Status
            };
        }
    }
}