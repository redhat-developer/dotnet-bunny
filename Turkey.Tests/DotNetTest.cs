using System;
using Xunit;

namespace Turkey.Tests
{
    public class DotNetTest
    {
        [Fact]
        public void GetRuntimeVersions()
        {
            var runtimeVersions = new DotNet().RuntimeVersions;
            Assert.NotNull(runtimeVersions);
            Assert.NotEmpty(runtimeVersions);
        }

        [Fact]
        public void GetLatestRuntimeVersion()
        {
            var runtimeVersion = new DotNet().LatestRuntimeVersion;
            Assert.NotNull(runtimeVersion);
        }

        [Fact]
        public void GetSdkVersions()
        {
            var sdkVersions = new DotNet().SdkVersions;
            Assert.NotNull(sdkVersions);
            Assert.NotEmpty(sdkVersions);
        }

        [Fact]
        public void GetLatestSdkVersion()
        {
            var sdkVersion = new DotNet().LatestSdkVersion;
            Assert.NotNull(sdkVersion);
        }
    }
}
