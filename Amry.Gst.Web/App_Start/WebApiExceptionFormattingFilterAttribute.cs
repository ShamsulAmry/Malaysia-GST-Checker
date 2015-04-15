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

            var customsEx = ex as CustomsGstException;
            if (customsEx != null) {
                if (customsEx.KnownErrorCode == KnownCustomsGstErrorCode.Over100Results) {
                    context.Response = new HttpResponseMessage(HttpStatusCode.Forbidden) {
                        ReasonPhrase = Resources.WebApiCustomsGstExceptionReasonPhrase,
                        Content = new StringContent(Resources.WebApiOver100Results)
                    };

                    context.Exception = null;
                } else {
                    context.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                        ReasonPhrase = Resources.WebApiCustomsGstExceptionReasonPhrase,
                        Content = new StringContent(ex.Message)
                    };
                }

                base.OnException(context);
                return;
            }

            var validationEx = ex as InvalidGstInputException;
            if (validationEx != null) {
                context.Response = new HttpResponseMessage(HttpStatusCode.BadRequest) {
                    ReasonPhrase = Resources.WebApiInvalidInputReasonPhrase,
                    Content = new StringContent(ex.Message)
                };

                context.Exception = null;
                base.OnException(context);
                return;
            }

            context.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                Content = new StringContent(ex.GetType().Name + " - " + ex.Message)
            };
            base.OnException(context);
        }
    }
}