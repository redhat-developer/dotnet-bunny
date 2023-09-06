using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Turkey
{
    public class TestParser
    {
        public Task<(bool Success, Test Test)> TryParseAsync(SystemUnderTest system, string nuGetConfig, FileInfo testConfiguration)
        {
            var dir = testConfiguration.Directory;
            return TryParseAsync(system, nuGetConfig, dir, File.ReadAllText(testConfiguration.FullName));
        }

        public async Task<(bool Success, Test Test)> TryParseAsync(SystemUnderTest system, string nuGetConfig, DirectoryInfo directory, string testConfiguration)
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
                    test = new XUnitTest(directory, system, nuGetConfig, descriptor, enabled);
                    return (true, test);
                case "bash":
                    test = new BashTest(directory, system, nuGetConfig, descriptor, enabled);
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

            if (system.SdkVersion == null && test.RequiresSdk)
            {
                return false;
            }

            if (test.IgnoredRIDs != null)
            {
                var skipped = system.CurrentPlatformIds
                    .Where(rid => test.IgnoredRIDs.Contains(rid))
                    .Any();

                if (skipped)
                {
                    return false;
                }
            }

            foreach (var skipCondition in test.SkipWhen)
            {
                // a skipCondition is formatted as comma-separated traits: 'green,age=21'
                // the condition is true when all traits are present in the test environment.

                bool skipConditionMatches = true;

                foreach (var skipConditionTrait in skipCondition.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                                .Select(s => s.Trim())
                                                                .Where(s => s.Length > 0))
                {
                    if (!system.Traits.Contains(skipConditionTrait))
                    {
                        skipConditionMatches = false;
                        break;
                    }
                }

                if (skipConditionMatches)
                {
                    return false;
                }
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
