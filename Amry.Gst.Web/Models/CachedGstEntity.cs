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
        public const string PartitionKeyPrefixForBusinessNameQuery = "NAME:";

        [DataMember]
        public string GstNumber { get; set; }

        [DataMember]
        public string BusinessName { get; set; }

        [DataMember]
        public string CommenceDate { get; set; }

        [DataMember]
        public string Status { get; set; }

        public string KnownErrorCode { get; set; }

        public bool IsLiveData
        {
            get { return false; }
        }

        public static string GetRowKeyForGstNumber(string gstNumber)
        {
            return gstNumber;
        }

        public static string GetRowKeyForBusinessRegNumber(string businessRegNumber)
        {
            return new string(businessRegNumber.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        }

        public static string GetPartitionKeyForBusinessNameQuery(string businessNameQuery)
        {
            return PartitionKeyPrefixForBusinessNameQuery + businessNameQuery.ToUpperInvariant().Replace(' ', '_');
        }

        public static CachedGstEntity CreateForGstNumberQuery(IGstLookupResult other)
        {
            return new CachedGstEntity {
                PartitionKey = PartitionKeyForGstNumber,
                RowKey = GetRowKeyForGstNumber(other.GstNumber),
                GstNumber = other.GstNumber,
                BusinessName = other.BusinessName,
                CommenceDate = other.CommenceDate,
                Status = other.Status
            };
        }

        public static CachedGstEntity CreateForBusinessRegNumberQuery(IGstLookupResult other, string businessRegNumber)
        {
            return new CachedGstEntity {
                PartitionKey = PartitionKeyForBusinessRegNumber,
                RowKey = GetRowKeyForBusinessRegNumber(businessRegNumber),
                GstNumber = other.GstNumber,
                BusinessName = other.BusinessName,
                CommenceDate = other.CommenceDate,
                Status = other.Status
            };
        }

        public static CachedGstEntity CreateForBusinessNameQuery(IGstLookupResult other, string businessName, int sequence)
        {
            return new CachedGstEntity {
                PartitionKey = GetPartitionKeyForBusinessNameQuery(businessName),
                RowKey = sequence.ToString("000"),
                GstNumber = other.GstNumber,
                BusinessName = other.BusinessName,
                CommenceDate = other.CommenceDate,
                Status = other.Status
            };
        }

        public static CachedGstEntity CreateForError(GstLookupInputType inputType, string input, KnownCustomsGstErrorCode error)
        {
            switch (inputType) {
                case GstLookupInputType.GstNumber:
                    return new CachedGstEntity {
                        PartitionKey = PartitionKeyForGstNumber,
                        RowKey = input,
                        KnownErrorCode = error.ToString()
                    };

                case GstLookupInputType.BusinessRegNumber:
                    return new CachedGstEntity {
                        PartitionKey = PartitionKeyForBusinessRegNumber,
                        RowKey = GetRowKeyForBusinessRegNumber(input),
                        KnownErrorCode = error.ToString()
                    };

                case GstLookupInputType.BusinessName:
                    return new CachedGstEntity {
                        PartitionKey = GetPartitionKeyForBusinessNameQuery(input),
                        RowKey = "000",
                        KnownErrorCode = error.ToString()
                    };
            }

            throw new NotSupportedException();
        }
    }
}