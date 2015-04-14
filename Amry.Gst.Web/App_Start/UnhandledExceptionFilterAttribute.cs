using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;

namespace Amry.Gst.Web
{
    public class UnhandledExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            context.Exception = new HttpResponseException(
                new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                    Content = new StringContent(context.Exception.GetType().Name + ": " + context.Exception.Message)
                });
            base.OnException(context);
        }
    }
}