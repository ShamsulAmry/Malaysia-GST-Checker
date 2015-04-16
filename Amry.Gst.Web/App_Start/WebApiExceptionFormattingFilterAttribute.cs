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
                    ReasonPhrase = Resources.WebApiCustomsGstExceptionReasonPhrase,
                    Content = new StringContent(ex.Message)
                };
            } else {
                context.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                    Content = new StringContent(ex.GetType().Name + " - " + ex.Message)
                };
            }

            base.OnException(context);
        }
    }
}