using System;
using Xunit;

namespace Samples
{
    public class FailingXUnitTest
    {
        [Fact]
        public void Test1()
        {
            Assert.True(false);
        }
    }
}
