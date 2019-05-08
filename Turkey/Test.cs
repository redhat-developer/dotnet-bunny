using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public TestStatus Status { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
    }

    public abstract class Test
    {
        public DirectoryInfo Directory { get; set; }
        public TestDescriptor Descriptor { get; set; }
        public bool Skip { get; set; }

        public Test(DirectoryInfo testDirectory, TestDescriptor descriptor)
        {
            this.Directory = testDirectory;
            this.Descriptor = descriptor;
        }

        public async Task<TestResult> RunAsync()
        {
            if (Skip)
            {
                return new TestResult()
                {
                    Status = TestStatus.Skipped,
                    StandardOutput = null,
                    StandardError = null,
                };
            }

            return await InternalRunAsync();
        }

        protected abstract Task<TestResult> InternalRunAsync();
    }
}
