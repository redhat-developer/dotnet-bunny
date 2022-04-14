using System.Linq;
using System;

using Xunit;

namespace Turkey.Tests
{
    public class PlatformIdTest
    {
        [Theory]
        [InlineData(new string[] { "ID=fedora", "VERSION_ID=9" }, "x64", "", new string[] { "linux", "linux-x64", "fedora", "fedora-x64", "fedora9", "fedora.9", "fedora.9-x64" })]
        [InlineData(new string[] { "ID=fedora", "VERSION_ID=30" }, "x64", "", new string[] { "linux", "linux-x64", "fedora", "fedora-x64", "fedora30", "fedora.30", "fedora.30-x64" })]
        [InlineData(new string[] { "ID=fedora", "VERSION_ID=30" }, "arm64", "", new string[] { "linux", "linux-arm64", "fedora", "fedora-arm64", "fedora30", "fedora.30", "fedora.30-arm64" })]
        [InlineData(new string[] { "ID=rocky", "VERSION_ID=8.5" }, "x64", "", new string[] { "linux", "linux-x64", "rocky", "rocky-x64", "rocky8", "rocky.8", "rocky.8-x64" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=7" }, "x64", "", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel7", "rhel.7", "rhel.7-x64" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=7.3" }, "x64", "", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel7", "rhel.7", "rhel.7-x64" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=8" }, "x64", "", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel8", "rhel.8", "rhel.8-x64" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=8.0" }, "x64", "", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel8", "rhel.8", "rhel.8-x64" })]
        [InlineData(new string[] { "ID=rhel", "VERSION_ID=8.1" }, "x64", "", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel8", "rhel.8", "rhel.8-x64" })]
        [InlineData(new string[] { "ID=\"rhel\"", "VERSION_ID=\"8.1\"" }, "x64", "", new string[] { "linux", "linux-x64", "rhel", "rhel-x64", "rhel8", "rhel.8", "rhel.8-x64" })]
        [InlineData(new string[] { "ID=centos", "VERSION_ID=8" }, "x64", "", new string[] { "linux", "linux-x64", "centos", "centos-x64", "centos8", "centos.8", "centos.8-x64" })]
        [InlineData(new string[] { "ID=alpine", "VERSION_ID=3.15.2" }, "x64", "", new string[] { "linux", "linux-x64", "alpine", "alpine-x64", "alpine3.15", "alpine.3.15", "alpine.3.15-x64" })]
        [InlineData(new string[] { "ID=alpine", "VERSION_ID=3.15.2" }, "x64", "musl libc", new string[] { "linux", "linux-x64", "alpine", "alpine-x64", "alpine3.15", "alpine.3.15", "alpine.3.15-x64", "linux-musl", "linux-musl-x64" })]
        [InlineData(new string[] { "ID=fedora", "VERSION_ID=39" }, "x64", "GNU libc", new string[] { "linux", "linux-x64", "fedora", "fedora-x64", "fedora39", "fedora.39", "fedora.39-x64" })]
        public void BasicPlatformIds(string[] osReleaseLines, string architecture, string lddOutput, string[] expectedIds)
        {
            var result = new PlatformId().ComputePlatformIds(osReleaseLines, architecture, lddOutput);
            Assert.Equal(expectedIds.ToList(), result);
        }

        [Fact]
        public void LddWorks()
        {
            string version = new PlatformId().GetLddVersion();
            Assert.NotNull(version);
            Assert.NotEmpty(version);
        }
    }
}
