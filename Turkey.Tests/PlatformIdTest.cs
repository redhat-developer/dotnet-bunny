using System.Linq;
using System;

using Xunit;

namespace Turkey.Tests
{
    public class PlatformIdTest
    {
        [Theory]
        [InlineData(new string[] { "ID=fedora", "VERSION_ID=9" }, "x64", new string[] { "linux", "linux-x64", "fedora", "fedora-x64", "fedora9", "fedora.9", "fedora.9-x64" })]
        [InlineData(new string[] { "ID=fedora", "VERSION_ID=30" }, "x64", new string[] { "linux", "linux-x64", "fedora", "fedora-x64", "fedora30", "fedora.30", "fedora.30-x64" })]
        [InlineData(new string[] { "ID=fedora", "VERSION_ID=30" }, "arm64", new string[] { "linux", "linux-arm64", "fedora", "fedora-arm64", "fedora30", "fedora.30", "fedora.30-arm64" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=7" }, "x64", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel7", "rhel.7", "rhel.7-x64" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=7.3" }, "x64", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel7", "rhel.7", "rhel.7-x64" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=8" }, "x64", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel8", "rhel.8", "rhel.8-x64" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=8.0" }, "x64", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel8", "rhel.8", "rhel.8-x64" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=8.1" }, "x64", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel8", "rhel.8", "rhel.8-x64" })]
        [InlineData(new string[] { "ID=\"rhel\"", "VERSION_ID=\"8.1\"" }, "x64", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel8", "rhel.8", "rhel.8-x64" })]
        [InlineData(new string[] { "ID=centos", "VERSION_ID=8" }, "x64", new string[] { "linux", "linux-x64", "centos", "centos-x64", "centos8", "centos.8", "centos.8-x64" })]
        public void BasicPlatformIds(string[] lines, string architecture, string[] expectedIds)
        {
            var result = new PlatformId().GetPlatformIdsFromOsRelease(lines, architecture);
            Assert.Equal(expectedIds.ToList(), result);
        }
    }
}
