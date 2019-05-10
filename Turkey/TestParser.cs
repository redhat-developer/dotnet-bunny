using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Turkey
{


    public class TestParser
    {
        public Task<Test> ParseAsync(SystemUnderTest system, FileInfo testConfiguration)
        {
            var dir = testConfiguration.Directory;
            return ParseAsync(system, dir, File.ReadAllText(testConfiguration.FullName));
        }

        public async Task<Test> ParseAsync(SystemUnderTest system, DirectoryInfo directory, string testConfiguration)
        {
            // TODO: async
            var fileName = Path.Combine(directory.FullName, "test.json");
            var descriptor = JsonConvert.DeserializeObject<TestDescriptor>(File.ReadAllText(fileName));

            if (!directory.Name.Equals(descriptor.Name, StringComparison.Ordinal))
            {
                Console.WriteLine($"Warning: mismatch in directory name vs test name in {descriptor.Name} test");
            }

            var enabled = ShouldRunTest(system, descriptor);
            var test = new BashTest(directory, descriptor, enabled);
            return test;
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
