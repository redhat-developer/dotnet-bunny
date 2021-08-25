using System;
using Xunit;

namespace Samples
{
    public class PassingXUnitTestWithEnvironmentVariables
    {
        [Fact]
        public void Test1()
        {
            Assert.Null(Environment.GetEnvironmentVariable("OPENSSL_CONF"));
        }
    }
}
