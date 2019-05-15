using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Turkey
{
    public class TestParser
    {
        public Task<(bool Success, Test Test)> TryParseAsync(SystemUnderTest system, FileInfo testConfiguration)
        {
            var dir = testConfiguration.Directory;
            return TryParseAsync(system, dir, File.ReadAllText(testConfiguration.FullName));
        }

        public async Task<(bool Success, Test Test)> TryParseAsync(SystemUnderTest system, DirectoryInfo directory, string testConfiguration)
        {
            // TODO: async
            var fileName = Path.Combine(directory.FullName, "test.json");
            var descriptor = JsonConvert.DeserializeObject<TestDescriptor>(File.ReadAllText(fileName));

            if (!directory.Name.Equals(descriptor.Name, StringComparison.Ordinal))
            {
                Console.WriteLine($"WARNING: mismatch in directory name (\"{directory.Name}\") vs test name (\"{descriptor.Name}\")");
            }

            var enabled = ShouldRunTest(system, descriptor);
            Test test = null;
            switch (descriptor.Type)
            {
                case "xunit":
                    test = new XUnitTest(directory, system, descriptor, enabled);
                    return (true, test);
                case "bash":
                    test = new BashTest(directory, system, descriptor, enabled);
                    return (true, test);
                default:
                    return (false, null);
            }
        }

        public bool ShouldRunTest(SystemUnderTest system, TestDescriptor test)
        {
            if (!test.Enabled)
            {
                return false;
            }

            if (!VersionMatches(test, system.RuntimeVersion))
            {
                return false;
            }

            var blacklisted = system.CurrentPlatformIds
                .Where(rid => test.PlatformBlacklist.Contains(rid))
                .Any();

            if (blacklisted)
            {
                return false;
            }

            return true;
        }


        private bool VersionMatches(TestDescriptor test, Version runtimeVersion)
        {
            if (test.VersionSpecific)
            {
                if ((runtimeVersion.Major + ".x").Equals(test.Version, StringComparison.Ordinal))
                {
                    return true;
                }
                else if (runtimeVersion.MajorMinor.Equals(test.Version, StringComparison.Ordinal))
                {
                    return true;
                }

                return false;
            }
            else
            {
                var testVersion = Version.Parse(test.Version);
                if (runtimeVersion >= testVersion)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
