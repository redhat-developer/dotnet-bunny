using System.Threading.Tasks;

using Xunit;

namespace Turkey.Tests
{
    public class ProgramTest
    {

        class MockNuGet : NuGet
        {
            public bool PackagesAreLive { get; set; }

            public MockNuGet(): base(null)
            {
            }

       }

        class MockSourceBuild : SourceBuild
        {
            public MockSourceBuild(): base(null)
            {

            }
        }

        [Fact]
        public void CheckOptions()
        {
            // TODO
        }

        [Fact]
        public void GetNuGetWorking()
        {

            var nuget = new MockNuGet();
            var sourceBuild = new MockSourceBuild();
            var netCoreAppVersion = Version.Parse("2.1.5");

            // TODO
            // var nugetConfig = Program.GetNuGetConfigIfNeeded(nuget, sourceBuild, netCoreAppVersion);
        }
    }
}
