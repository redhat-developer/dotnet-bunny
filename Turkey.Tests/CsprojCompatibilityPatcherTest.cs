using System;

using Turkey;

using Xunit;

namespace Turkey.Tests
{
    public class CsprojCompatibilityPatcherTest
    {
        [Theory]
        [InlineData("netcoreapp1.0", "2.1", "netcoreapp2.1")]
        [InlineData("netcoreapp2.1", "3.1", "netcoreapp3.1")]
        [InlineData("netcoreapp3.1", "3.1", "netcoreapp3.1")]
        [InlineData("net5.0", "3.1", "netcoreapp3.1")]
        [InlineData("net5.0", "2.1", "netcoreapp2.1")]
        public void TargetFrameworksAreReplacedCorrectly(string currentTfm, string runtimeVersion, string expectedTfm)
        {
            Version newRuntimeVersion = Version.Parse(runtimeVersion);
            string csproj = $@"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>{currentTfm}</TargetFramework>
  </PropertyGroup>

</Project>";
            string patched = new CsprojCompatibilityPatcher().Patch(csproj, newRuntimeVersion);
            Assert.Contains(expectedTfm, patched);
            if (currentTfm != expectedTfm)
            {
                Assert.DoesNotContain(currentTfm, patched);
            }
        }
    }
}
