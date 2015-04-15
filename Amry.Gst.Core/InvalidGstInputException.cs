using System;

namespace Amry.Gst
{
    public class InvalidGstInputException : Exception
    {
        public InvalidGstInputException(string message)
            : base(message)
        {}
    }
}