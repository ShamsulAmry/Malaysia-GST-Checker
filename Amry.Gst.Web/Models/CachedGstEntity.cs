using System;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.WindowsAzure.Storage.Table;

namespace Amry.Gst.Web.Models
{
    [DataContract]
    class CachedGstEntity : TableEntity, IGstLookupResult
    {
        public const string PartitionKeyForGstNumber = "GST";
        public const string PartitionKeyForBusinessRegNumber = "REG";

        [DataMember]
        public DateTimeOffset CacheTimestamp
        {
            get { return Timestamp.ToOffset(TimeSpan.FromHours(8)); }
        }

        [DataMember]
        public string GstNumber { get; set; }

        [DataMember]
        public string BusinessName { get; set; }

        [DataMember]
        public string CommenceDate { get; set; }

        [DataMember]
        public string Status { get; set; }

        public static string GetRowKeyForGstNumber(string gstNumber)
        {
            return gstNumber;
        }

        public static string GetRowKeyForBusinessRegNumber(string businessRegNumber)
        {
            return new string(businessRegNumber.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        }

        public static CachedGstEntity Create(IGstLookupResult other, string businessRegNumber = null)
        {
            return new CachedGstEntity {
                PartitionKey = businessRegNumber == null
                    ? PartitionKeyForGstNumber
                    : PartitionKeyForBusinessRegNumber,
                RowKey = businessRegNumber == null
                    ? GetRowKeyForGstNumber(other.GstNumber)
                    : GetRowKeyForBusinessRegNumber(businessRegNumber),
                GstNumber = other.GstNumber,
                BusinessName = other.BusinessName,
                CommenceDate = other.CommenceDate,
                Status = other.Status
            };
        }
    }
}