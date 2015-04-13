using System;
using System.IO;
using System.Threading.Tasks;

namespace Amry.Gst.DeleteCache
{
    static class TextWriterExtension
    {
        public static Task LogAsync(this TextWriter logger, string format, string arg0)
        {
            return logger.WriteLineAsync(
                DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss.fff") +
                    " " + string.Format(format, arg0));
        }
    }
}