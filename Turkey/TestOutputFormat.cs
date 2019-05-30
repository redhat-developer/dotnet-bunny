using System;
using System.Threading.Tasks;

namespace Turkey
{
    public class TestOutputFormats
    {
        public class DotNetBunnyOutput : TestOutput
        {
            private readonly LogWriter _logWriter;

            public DotNetBunnyOutput(LogWriter writer)
            {
                this._logWriter = writer;
            }

            public async override Task AtStartupAsync()
            {
                Console.WriteLine("\n\n(\\_/)\n(^_^)\n@(\")(\")\n\n".Replace("\n", Environment.NewLine));
            }

            public async override Task AfterParsingTestAsync(string name, bool enabled)
            {
                if (enabled)
                {
                    Console.WriteLine($"Running {name}");
                }
            }

            public async override Task AfterRunningTestAsync(string name, TestResult result)
            {
                string resultText;
                switch (result.Status)
                {
                    case TestStatus.Passed:
                        resultText = "PASS";
                        Console.WriteLine($"Result: {resultText}" + Environment.NewLine);
                        break;
                    case TestStatus.Failed:
                        resultText = "FAIL - Code: 1";
                        Console.WriteLine($"Result: {resultText}" + Environment.NewLine);
                        break;
                    default:
                        break;
                }

                if (result.Status == TestStatus.Failed)
                {
                    await _logWriter.WriteAsync(name, result.StandardOutput, result.StandardError);
                }

            }

            public async override Task AfterRunningAllTestsAsync(TestResults results)
            {
                Console.WriteLine($"Total: {results.Total} Passed: {results.Passed} Failed: {results.Failed}");
            }
        }

        public class NewOutput : TestOutput
        {

            private readonly LogWriter _logWriter;

            public NewOutput(LogWriter writer)
            {
                this._logWriter = writer;
            }

            public async override Task AfterParsingTestAsync(string name, bool enabled)
            {
                var nameText = string.Format("{0,-60}", name);
                Console.Write(nameText);
            }

            public async override Task AfterRunningTestAsync(string name, TestResult result)
            {
                string resultOutput = null;
                if (Console.IsOutputRedirected || Console.IsErrorRedirected)
                {
                    switch (result.Status)
                    {
                        case TestStatus.Passed: resultOutput = "PASS"; break;
                        case TestStatus.Failed: resultOutput = "FAIL"; break;
                        case TestStatus.Skipped: resultOutput = "SKIP"; break;
                    }
                    Console.WriteLine($"[{resultOutput}]");
                }
                else
                {
                    switch (result.Status)
                    {
                        case TestStatus.Passed: resultOutput = "\u001b[32mPASS\u001b[0m"; break;
                        case TestStatus.Failed: resultOutput = "\u001b[31mFAIL\u001b[0m"; break;
                        case TestStatus.Skipped: resultOutput = "SKIP"; break;
                    }
                    Console.WriteLine($"[{resultOutput}]");
                }

                if (result.Status == TestStatus.Failed)
                {
                    await _logWriter.WriteAsync(name, result.StandardOutput, result.StandardError);
                }
            }

            public async override Task AfterRunningAllTestsAsync(TestResults results)
            {
                Console.WriteLine($"Total: {results.Total} Passed: {results.Passed} Failed: {results.Failed}");
            }
        }
    }
}
