using System;
using System.Threading;
using Xunit;

namespace Samples
{
    public class PassingXUnitTest
    {
        [Fact]
        public void Test1()
        {
            TimeSpan duration = new TimeSpan(hours: 1, minutes: 0, seconds: 0);
            Thread.Sleep(duration);
        }
    }
}
