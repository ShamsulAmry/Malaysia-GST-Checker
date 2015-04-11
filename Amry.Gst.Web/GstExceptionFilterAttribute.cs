﻿using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;
using Amry.Gst.Web.Properties;

namespace Amry.Gst.Web
{
    public class GstExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            var ex = context.Exception as GstException;
            if (ex == null) {
                base.OnException(context);
                return;
            }

            context.Exception = new HttpResponseException(
                new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                    ReasonPhrase = Resources.GstExceptionHttpResponseReasonPhrase,
                    Content = new StringContent(ex.Message)
                });
            base.OnException(context);
        }
    }
}