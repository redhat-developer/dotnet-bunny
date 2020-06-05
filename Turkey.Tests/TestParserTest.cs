using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Turkey.Tests
{
    public class TestParserTests
    {

        [Fact]
        public void DisabledTestShouldBeSkipped()
        {
            TestParser parser = new TestParser();
            SystemUnderTest system = new SystemUnderTest(null, null, null);
            TestDescriptor test = new TestDescriptor()
            {
                Enabled = false,
            };

            var shouldRun = parser.ShouldRunTest(system, test);

            Assert.False(shouldRun);
        }

        [Theory]
        [InlineData("1.0.1", false)]
        [InlineData("1.1", false)]
        [InlineData("1.1.1", false)]
        [InlineData("2.0", false)]
        [InlineData("2.0.9", false)]
        [InlineData("2.1", true)]
        [InlineData("2.1.0", true)]
        [InlineData("2.1.1", true)]
        [InlineData("2.2", true)]
        [InlineData("2.3", true)]
        [InlineData("3.0", true)]
        public void TestShouldBeRunForSameOrHigherVersions(string version, bool expectedToRun)
        {
            TestParser parser = new TestParser();
            SystemUnderTest system = new SystemUnderTest(
                runtimeVersion: Version.Parse(version),
                sdkVersion: null,
                platformIds: new List<string>());

            TestDescriptor test = new TestDescriptor()
            {
                Enabled = true,
                RequiresSdk = false,
                VersionSpecific = false,
                Version = "2.1",
            };

            var shouldRun = parser.ShouldRunTest(system, test);

            Assert.Equal(expectedToRun, shouldRun);
        }

        [Theory]
        [InlineData("1.0.1", false)]
        [InlineData("1.1", false)]
        [InlineData("1.1.1", false)]
        [InlineData("2.0", false)]
        [InlineData("2.0.9", false)]
        [InlineData("2.1", true)]
        [InlineData("2.1.0", true)]
        [InlineData("2.1.1", true)]
        [InlineData("2.1.99", true)]
        [InlineData("2.2", false)]
        [InlineData("2.3", false)]
        [InlineData("3.0", false)]
        public void VersionSpecificTestShouldBeRunForSameMajorMinorVersion(string version, bool expectedToRun)
        {
            TestParser parser = new TestParser();
            SystemUnderTest system = new SystemUnderTest(
                runtimeVersion: Version.Parse(version),
                sdkVersion: null,
                platformIds: new List<string>());
            TestDescriptor test = new TestDescriptor()
            {
                Enabled = true,
                RequiresSdk = false,
                VersionSpecific = true,
                Version = "2.1",
            };

            var shouldRun = parser.ShouldRunTest(system, test);

            Assert.Equal(expectedToRun, shouldRun);
        }

        [Theory]
        [InlineData("1.0.1", false)]
        [InlineData("1.1", false)]
        [InlineData("1.1.1", false)]
        [InlineData("2.0", true)]
        [InlineData("2.0.9", true)]
        [InlineData("2.1", true)]
        [InlineData("2.1.0", true)]
        [InlineData("2.1.1", true)]
        [InlineData("2.1.99", true)]
        [InlineData("2.2", true)]
        [InlineData("2.2.99", true)]
        [InlineData("2.3", true)]
        [InlineData("2.9", true)]
        [InlineData("3.0", false)]
        [InlineData("3.2", false)]
        public void VersionSpecificTestWithWildcardShouldBeRunForSameMajorVersion(string version, bool expectedToRun)
        {
            TestParser parser = new TestParser();
            SystemUnderTest system = new SystemUnderTest(
                runtimeVersion: Version.Parse(version),
                sdkVersion: null,
                platformIds: new List<string>());
            TestDescriptor test = new TestDescriptor()
            {
                Enabled = true,
                RequiresSdk = false,
                VersionSpecific = true,
                Version = "2.x",
            };

            var shouldRun = parser.ShouldRunTest(system, test);

            Assert.Equal(expectedToRun, shouldRun);
        }

        [Theory]
        [InlineData(new string[] { "linux" }, new string[] { }, true)]
        [InlineData(new string[] { "linux" }, new string[] { "fedora" }, true)]
        [InlineData(new string[] { "fedora" }, new string[] { }, true)]
        [InlineData(new string[] { "fedora99" }, new string[] { }, true)]
        [InlineData(new string[] { "fedora99" }, new string[] { "fedora10" }, true)]
        [InlineData(new string[] { "fedora" }, new string[] { "fedora" }, false)]
        [InlineData(new string[] { "fedora", "fedora99" }, new string[] { "fedora" }, false)]
        public void TestShouldNotRunOnBlacklistedPlatforms(string[] currentPlatforms, string[] platformBlacklist, bool expectedToRun)
        {
            TestParser parser = new TestParser();
            SystemUnderTest system = new SystemUnderTest(
                runtimeVersion: Version.Parse("2.1"),
                sdkVersion: null,
                platformIds: currentPlatforms.ToList());
            TestDescriptor test = new TestDescriptor()
            {
                Enabled = true,
                RequiresSdk = false,
                Version = "2.1",
                PlatformBlacklist = platformBlacklist.ToList(),
            };

            var shouldRun = parser.ShouldRunTest(system, test);

            Assert.Equal(expectedToRun, shouldRun);
        }

        [Theory]
        [InlineData("3.1.104", true, true)]
        [InlineData(null, false, true)]
        [InlineData(null, true, false)]
        public void SdkTestsShouldRunOnlyWithSdk(string sdkVersion, bool requiresSdk, bool expectedToRun)
        {
            TestParser parser = new TestParser();
            SystemUnderTest system = new SystemUnderTest(
                runtimeVersion: Version.Parse("3.1"),
                sdkVersion: Version.Parse(sdkVersion),
                platformIds: new List<string>());
            TestDescriptor test = new TestDescriptor()
            {
                Enabled = true,
                RequiresSdk = requiresSdk,
                VersionSpecific = false,
                Version = "2.1",
            };

            var shouldRun = parser.ShouldRunTest(system, test);

            Assert.Equal(expectedToRun, shouldRun);
        }
    }
}
