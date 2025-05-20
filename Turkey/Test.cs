using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Turkey
{
    public class TestDescriptor
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public bool RequiresSdk { get; set; }
        public string Version { get; set; }
        public bool VersionSpecific { get; set; }
        public string Type { get; set; }
        public bool Cleanup { get; set; }
        public double TimeoutMultiplier { get; set; } = 1.0;

        #pragma warning disable CA2227 // Change to be read-only by removing the property setter.
        public List<string> IgnoredRIDs { get; set; } = new();
        public List<string> SkipWhen { get; set; } = new();

        #pragma warning restore CA2227
    }

    // TODO is this a strongly-typed enum in C#?
    public enum TestResult {
        Passed, Failed, Skipped,
    }

    public abstract class Test
    {
        public DirectoryInfo Directory { get; }
        public SystemUnderTest SystemUnderTest { get; }
        public string NuGetConfig { get; }
        public TestDescriptor Descriptor { get; }
        public bool Skip { get; }

        public Test(DirectoryInfo testDirectory, SystemUnderTest system, string nuGetConfig, TestDescriptor descriptor, bool enabled)
        {
            this.Directory = testDirectory;
            this.SystemUnderTest = system;
            this.NuGetConfig = nuGetConfig;
            this.Descriptor = descriptor;
            this.Skip = !enabled;
        }

        public async Task<TestResult> RunAsync(Action<string> logger, CancellationToken cancelltionToken)
        {
            if (Skip)
            {
                return TestResult.Skipped;
            }

            var path = Path.Combine(Directory.FullName, "nuget.config");
            if (!string.IsNullOrEmpty(NuGetConfig))
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"WARNING: overwriting {path}");
                }
                await File.WriteAllTextAsync(path, NuGetConfig).ConfigureAwait(false);
            }

            var testResult = await InternalRunAsync(logger, cancelltionToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(NuGetConfig))
            {
                File.Delete(path);
            }

            return testResult;
        }

        protected abstract Task<TestResult> InternalRunAsync(Action<string> logger, CancellationToken cancellationToken);

    }
}
