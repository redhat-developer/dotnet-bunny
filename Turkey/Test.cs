using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Turkey
{
    public class TestDescriptor
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public string Version { get; set; }
        public bool VersionSpecific { get; set; }
        public string Type { get; set; }
        public bool Cleanup { get; set; }
        public List<string> PlatformBlacklist { get; set; }
    }

    // TODO is this a strongly-typed enum in C#?
    public enum TestStatus {
        Passed, Failed, Skipped,
    }

    public class TestResult
    {
        public TestStatus Status { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }

        public TestResult(TestStatus status, string standardOutput, string standardError)
        {
            Status = status;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }
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

        public async Task<TestResult> RunAsync(CancellationToken cancelltionToken)
        {
            if (Skip)
            {
                return new TestResult(
                    status: TestStatus.Skipped,
                    standardOutput: null,
                    standardError: null);
            }

            var path = Path.Combine(Directory.FullName, "nuget.config");
            if (!string.IsNullOrEmpty(NuGetConfig))
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"WARNING: overwriting {path}");
                }
                await File.WriteAllTextAsync(path, NuGetConfig);
            }

            var result = await InternalRunAsync(cancelltionToken);

            if (!string.IsNullOrEmpty(NuGetConfig))
            {
                File.Delete(path);
            }

            return result;
        }

        protected abstract Task<TestResult> InternalRunAsync(CancellationToken cancellationToken);
    }
}
