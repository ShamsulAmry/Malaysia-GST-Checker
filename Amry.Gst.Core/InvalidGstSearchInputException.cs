using System;

namespace Amry.Gst
{
    public class InvalidGstSearchInputException : Exception
    {
        public InvalidGstSearchInputException(string message) : base(message)
        { }
    }
}