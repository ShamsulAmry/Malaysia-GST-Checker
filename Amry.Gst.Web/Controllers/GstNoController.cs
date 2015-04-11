﻿using System.Web.Http;
using Amry.Gst.Web.Models;

namespace Amry.Gst.Web.Controllers
{
    public class GstNoController : ApiController
    {
        readonly IGstDataSource _gstDataSource;

        public GstNoController(IGstDataSource gstDataSource)
        {
            _gstDataSource = gstDataSource;
        }

        public IHttpActionResult Get(string id)
        {
            return new PossiblyCachedResult(_gstDataSource.LookupGstData(GstLookupInputType.GstNumber, id));
        }
    }
}