using System;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Turkey
{
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
        public string Name { get; set; }
        public bool Skip { get; set; }

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
