﻿using System;
using System.Collections.Generic;
using System.Web.Http;
using Distracey.MethodHandler;

namespace Distracey.Examples.ServiceDepthThree.Controllers
{
    public class DepthThreeController : ApiController
    {
        public IEnumerable<string> GetDepthThree(int id)
        {
            Request.ApmContext()["id"] = id.ToString();

            return ReadFromFakeDatabaseForDepthThree();
        }

        private IEnumerable<string> ReadFromFakeDatabaseForDepthThree()
        {
            var apmContext = ApmContext.GetContext();
            var methodHandler = apmContext.GetMethodHander();
            return methodHandler.Execute<IEnumerable<string>>(() => new[] { "two" });
        }

        public IEnumerable<string> GetDepthThreeException(int id)
        {
            Request.ApmContext()["id"] = id.ToString();

            throw new Exception("three exception");
        }
    }
}