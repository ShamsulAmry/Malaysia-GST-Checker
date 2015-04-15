using System;

namespace Amry.Gst
{
    public class CustomsGstException : Exception
    {
        public CustomsGstException(string message,
            KnownCustomsGstErrorCode? knownErrorCode = null,
            Exception innerException = null)
            : base(message, innerException)
        {
            KnownErrorCode = knownErrorCode;
        }

        public KnownCustomsGstErrorCode? KnownErrorCode { get; private set; }
    }

    public enum KnownCustomsGstErrorCode
    {
        NoResult,
        Over100Results
    }
}