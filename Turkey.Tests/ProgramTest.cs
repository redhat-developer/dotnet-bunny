using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
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

        public static IEnumerable<object[]> SystemTraits_MemberData()
        {
            // note: the version numbers are intentionally chosen to be different.
            Version runtimeVersion = Version.Parse("6.5");
            Version sdkVersion = Version.Parse("3.1");

            string[] expectedVersionTraits = new[] { "version=6.5", "version=6" };
            string expectedArch = $"arch={OSArchitectureName}";

            // default traits.
            yield return new object[] { runtimeVersion, sdkVersion, Array.Empty<string>(), false, Array.Empty<string>(), CombineTraits() };

            // 'runtime=mono'
            yield return new object[] { runtimeVersion, sdkVersion, Array.Empty<string>(), true, Array.Empty<string>(), CombineTraits(isMonoRuntime: true) };

            // 'os=..' and 'rid=...' are added for the platform rids.
            yield return new object[] { runtimeVersion, sdkVersion, new[] { "linux-x64", "fedora.37-x64", "linux-musl-x64" }, false, Array.Empty<string>(),
                                    CombineTraits(new[] { "os=linux", "os=fedora.37", "os=linux-musl",
                                                          "rid=linux-x64", "rid=fedora.37-x64", "rid=linux-musl-x64" } ) };

            // additional traits are added.
            yield return new object[] { runtimeVersion, sdkVersion, Array.Empty<string>(), false, new[] { "blue", "green" },
                                            CombineTraits(new[] { "blue", "green" } ) };

            string[] CombineTraits(string[] expectedAdditionalTraits = null, bool isMonoRuntime = false)
                => expectedVersionTraits
                    .Concat(new[] { expectedArch })
                    .Concat(isMonoRuntime ? new[] { "runtime=mono" } : new[] { "runtime=coreclr" })
                    .Concat(expectedAdditionalTraits ?? Array.Empty<string>())
                    .ToArray();
        }

        [Theory]
        [MemberData(nameof(SystemTraits_MemberData))]
        public void SystemTraits(Version runtimeVersion, Version sdkVersion, string[] rids, bool isMonoRuntime, string[] additionalTraits, string[] expectedTraits)
        {
            IReadOnlySet<string> systemTraits = Program.CreateTraits(runtimeVersion, sdkVersion, new List<string>(rids), isMonoRuntime, additionalTraits);

            Assert.Equal(expectedTraits.OrderBy(s => s), systemTraits.OrderBy(s => s));
        }

        private static string OSArchitectureName
            => RuntimeInformation.OSArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm => "arm",
                Architecture.Arm64 => "arm64",
                Architecture.S390x => "s390x",
                (Architecture)8 => "ppc64le", // not defined for 'net6.0' target.
                _ => throw new NotSupportedException(),
            };
    }
}
