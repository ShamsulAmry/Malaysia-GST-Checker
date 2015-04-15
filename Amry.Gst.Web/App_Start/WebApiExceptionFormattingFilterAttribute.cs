using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using Amry.Gst.Web.Properties;

namespace Amry.Gst.Web
{
    public class WebApiExceptionFormattingFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            var ex = context.Exception;

            if (ex is CustomsGstException) {
                context.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                    ReasonPhrase = Resources.CustomsGstExceptionHttpResponseReasonPhrase,
                    Content = new StringContent(ex.Message)
                };
            } else if (ex is InternalGstException) {
                context.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                    ReasonPhrase = Resources.InternalGstExceptionHttpResponseReasonPhrase,
                    Content = new StringContent(ex.Message)
                };
            } else {
                context.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                    Content = new StringContent(context.Exception.GetType().Name + " - " + ex.Message)
                };
            }

            base.OnException(context);
        }
    }
}