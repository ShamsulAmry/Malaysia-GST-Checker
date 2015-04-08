namespace Amry.Gst
{
    public class GstJsonResult
    {
        public GstWebUpdates Updates { get; set; }
    }

    public class GstWebUpdates
    {
        public GstWebFieldUpdate[] FieldUpdates { get; set; }
        public GstWebViewUpdate[] ViewUpdates { get; set; }
    }

    public class GstWebFieldUpdate
    {
        public string Container { get; set; }
        public bool? Enabled { get; set; }
        public string Field { get; set; }
        public string FieldClass { get; set; }
        public string IndicatorClass { get; set; }
        public bool? InError { get; set; }
        public bool? IsTable { get; set; }
        public string Message { get; set; }
        public string MessageClass { get; set; }
        public int? TabIndex { get; set; }
        public string Tooltip { get; set; }
        public string Value { get; set; }
        public bool? Visible { get; set; }
        public string Watermark { get; set; }
    }

    public class GstWebViewUpdate
    {
        public bool? InError { get; set; }
        public string View { get; set; }
    }

}