﻿using System.Reflection.Emit;
using System.Web.Http;
using System.Web.Http.Results;
using Amry.Gst.Web.Models;

namespace Amry.Gst.Web.Controllers
{
    public class GstNoController : GstApiController
    {
        readonly IGstDataSource _gstDataSource;

        public GstNoController(IGstDataSource gstDataSource)
        {
            _gstDataSource = gstDataSource;
        }

        public IHttpActionResult Get(string id)
        {
            return new PossiblyCachedResult(_gstDataSource.LookupGstDataAsync(
                GstLookupInputType.GstNumber, id, true));
        }
    }
}