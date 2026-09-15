using System;
using Xunit;

namespace Samples
{
    public class PassingXUnitTestWithEnvironmentVariables
    {
        [Fact]
        public void Test1()
        {
            Assert.Null(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
            Assert.Null(Environment.GetEnvironmentVariable("COREHOST_TRACE"));
            Assert.Null(Environment.GetEnvironmentVariable("DOTNET_FOO_BAR"));
            Assert.Null(Environment.GetEnvironmentVariable("DOTNET_ROLL_FORWARD"));
            Assert.Null(Environment.GetEnvironmentVariable("DOTNET_RUNTIME_ID"));
            Assert.Null(Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS"));
            Assert.Null(Environment.GetEnvironmentVariable("NUGET_PACKAGES"));
            Assert.Null(Environment.GetEnvironmentVariable("OPENSSL_CONF"));
        }
    }
}
