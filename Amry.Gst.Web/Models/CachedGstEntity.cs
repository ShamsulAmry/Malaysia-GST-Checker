using System;
using System.Linq;
using System.Runtime.Serialization;
using Amry.Gst.Web.Properties;
using Microsoft.WindowsAzure.Storage.Table;

namespace Amry.Gst.Web.Models
{
    [DataContract]
    class CachedGstEntity : TableEntity, IGstLookupResult
    {
        const string PartitionKeyPrefixForGstNumber = "GST:";
        const string PartitionKeyPrefixForBusinessRegNumber = "REG:";
        const string PartitionKeyPrefixForBusinessNameQuery = "NAME:";

        public string KnownErrorCode { get; set; }

        [DataMember]
        public string GstNumber { get; set; }

        [DataMember]
        public string BusinessName { get; set; }

        [DataMember]
        public string CommenceDate { get; set; }

        [DataMember]
        public string Status { get; set; }

        public bool IsLiveData
        {
            get { return false; }
        }

        public static string GetPartitionKey(GstLookupInputType inputType, string input)
        {
            switch (inputType) {
                case GstLookupInputType.GstNumber:
                    return PartitionKeyPrefixForGstNumber + input;

                case GstLookupInputType.BusinessRegNumber:
                    return PartitionKeyPrefixForBusinessRegNumber + new string(
                        input.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

                case GstLookupInputType.BusinessName:
                    return PartitionKeyPrefixForBusinessNameQuery +
                        input.ToUpperInvariant().Replace(' ', '_');

                default:
                    throw new NotSupportedException();
            }
        }

        public static CachedGstEntity CreateForResult(GstLookupInputType inputType, string input, IGstLookupResult liveResult, int sequence)
        {
            if (!liveResult.IsLiveData) {
                throw new ArgumentException(Resources.WebApiCannotCacheStaleData, "liveResult");
            }

            return new CachedGstEntity {
                PartitionKey = GetPartitionKey(inputType, input),
                RowKey = sequence.ToString("000"),
                GstNumber = liveResult.GstNumber,
                BusinessName = liveResult.BusinessName,
                CommenceDate = liveResult.CommenceDate,
                Status = liveResult.Status
            };
        }

        public static CachedGstEntity CreateForError(GstLookupInputType inputType, string input, KnownCustomsGstErrorCode error)
        {
            return new CachedGstEntity {
                PartitionKey = GetPartitionKey(inputType, input),
                RowKey = "000",
                KnownErrorCode = error.ToString()
            };
        }
    }
}