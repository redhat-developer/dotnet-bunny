using System;
using Xunit;

namespace Samples
{
    public class PassingXUnitTest
    {
        [Fact]
        public void Test1()
        {
            Assert.True(true);
        }
    }
}
