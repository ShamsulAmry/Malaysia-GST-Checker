using System;

namespace Amry.Gst
{
    public class InternalGstException : Exception
    {
        public InternalGstException(string message) : base(message)
        { }
    }
}