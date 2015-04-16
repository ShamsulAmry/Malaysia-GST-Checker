using System.Web.Http.Filters;

namespace Amry.Gst.Web
{
    class RecordMissingCustomsTokenExceptionInNewRelicAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            var ex = context.Exception as MissingCustomsTokenException;
            if (ex != null) {
                NewRelic.Api.Agent.NewRelic.NoticeError(ex, ex.ResponseDetails);
            }

            base.OnException(context);
        }
    }
}