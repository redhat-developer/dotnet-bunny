using System.Linq;

using Xunit;

namespace Turkey.Tests
{
    public class PlatformIdTest
    {
        [Theory]
        [InlineData(new string[] { "ID=fedora", "VERSION_ID=10" }, new string[] { "linux", "fedora", "fedora10" })]
        [InlineData(new string[] { "ID=fedora", "VERSION_ID=30" }, new string[] { "linux", "fedora", "fedora30" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=7" }, new string[] { "linux", "rhel", "rhel7" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=7.3" }, new string[] { "linux", "rhel", "rhel7" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=8" }, new string[] { "linux", "rhel", "rhel8" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=8.0" }, new string[] { "linux", "rhel", "rhel8" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=8.1" }, new string[] { "linux", "rhel", "rhel8" })]
        [InlineData(new string[] { "ID=centos", "VERSION_ID=8" }, new string[] { "linux", "centos", "centos8" })]
        public void BasicPlatformIds(string[] lines, string[] expectedIds)
        {
            var result = new PlatformId().GetPlatformIdsFromOsRelease(lines);

            Assert.Equal(expectedIds.ToList(), result);
        }
    }
}
