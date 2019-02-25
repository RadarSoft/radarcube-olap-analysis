using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest.OlapAnalysis.Mock
{
    public class HostingMock
    {
        public HostingMock()
        {
            Hosting = new Mock<HostingTest>(MockBehavior.Strict);
        }

        public Mock<HostingTest> Hosting { get; set; }

    }

    public class HostingTest : IHostingEnvironment
    {
        public string EnvironmentName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string WebRootPath
        {
            get => AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin"));
            set => throw new NotImplementedException();
        }
        public IFileProvider WebRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ContentRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
