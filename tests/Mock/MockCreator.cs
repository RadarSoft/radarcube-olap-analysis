using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest.OlapAnalysis.Mock
{
    public static class MockCreator
    {
        public static HttpContext CreateContext()
        {
            var parameters = new Dictionary<string, StringValues>
            {
                { "data", new StringValues("") }
            };

            var mockContext = new HttpContextMock(parameters);
            return mockContext.HttpContext;
        }

        public static IHostingEnvironment CreateHosting()
        {
            var moqHosting = new HostingMock();
            return moqHosting.Hosting.Object;
        }

    }
}
