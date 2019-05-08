using System;
using Xunit;

using Version = Turkey.Version;

namespace Turkey.Tests
{
    public class VersionTest
    {
        [Fact]
        public void SimpleParse()
        {
            var version = Version.Parse("2.1");
            Assert.NotNull(version);
            Assert.Equal(2, version.Major);
            Assert.Equal(1, version.Minor);
            Assert.Equal("2.1", version.MajorMinor);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1.0")]
        [InlineData("1.0.0")]
        [InlineData("1.0.0.0")]
        [InlineData("1.0.0.preview3")]
        [InlineData("1.0.0.a1")]
        [InlineData("1.0.0.a.1")]
        public void ParseableVersions(string input)
        {
            Version.Parse(input);
        }

        [Fact]
        public void Equality()
        {
            var v1 = Version.Parse("1.0");
            var v2 = Version.Parse("1.0");

            Assert.Equal(v1, v2);
            Assert.True(v1 == v2);
            Assert.False(v1 != v2);
        }

        [Theory]
        [InlineData(".1")]
        public void InvalidVersionsThrow(string version)
        {
            Assert.Throws<FormatException>(() => Version.Parse(version));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1.0")]
        [InlineData("1.0.0")]
        [InlineData("1.0.0.0.0")]
        [InlineData("1.0.0.0.0.0")]
        public void TrailingZerosDoNotMatter(string version)
        {
            var v1 = Version.Parse("1.0");
            var v2 = Version.Parse(version);

            Assert.Equal(v1, v2);
            Assert.True(v1 == v2);
            Assert.False(v1 != v2);
        }

        [Theory]
        [InlineData("0.0.1")]
        [InlineData("0.0.0.1")]
        public void LeadingZerosMatter(string version)
        {
            var v1 = Version.Parse("0.1");
            var v2 = Version.Parse(version);

            Assert.NotEqual(v1, v2);
            Assert.False(v1 == v2);
            Assert.True(v1 != v2);
        }

        [Fact]
        public void VersionComparisons()
        {
            var v1 = Version.Parse("1.0");
            var v2 = Version.Parse("1.0.1");
            var v3 = Version.Parse("1.0.2");
            var v4 = Version.Parse("0.1.2");
            var v5 = Version.Parse("0.9.2");
            var v6 = Version.Parse("0.10.2");

            Assert.True(v1 < v2);
            Assert.True(v2 < v3);
            Assert.True(v3 > v2);
            Assert.True(v2 > v1);
            Assert.True(v4 < v1);
            Assert.True(v4 < v5);
            Assert.True(v6 > v5);
            Assert.True(v6 > v4);
            Assert.True(v6 < v1);
        }

        [Fact]
        public void TestToString()
        {
            var v1 = Version.Parse("1.0");
            Assert.Equal("1.0", v1.ToString());
        }
    }
}
