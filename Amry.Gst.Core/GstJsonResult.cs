namespace Amry.Gst
{
    public class GstJsonResult
    {
        public GstWebUpdates Updates;
    }

    public class GstWebUpdates
    {
        public GstWebFieldUpdate[] FieldUpdates;
        public GstWebViewUpdate[] ViewUpdates;
    }

    public class GstWebFieldUpdate
    {
        public string Container;
        public bool? Enabled;
        public string Field;
        public string FieldClass;
        public string IndicatorClass;
        public bool? InError;
        public bool? IsTable;
        public string Message;
        public string MessageClass;
        public int? TabIndex;
        public string Tooltip;
        public string Value;
        public bool? Visible;
        public string Watermark;
    }

    public class GstWebViewUpdate
    {
        public bool? InError;
        public string View;
    }

}