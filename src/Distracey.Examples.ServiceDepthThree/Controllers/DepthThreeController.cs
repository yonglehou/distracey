﻿using System.Collections.Generic;
using System.Web.Http;

namespace Distracey.Examples.ServiceDepthThree.Controllers
{
    public class DepthThreeController : ApiController
    {
        public IEnumerable<string> GetDepthThree(int id)
        {
            Request.ApmContext()["id"] = id.ToString();

            return new[] { "three" };
        }
    }
}