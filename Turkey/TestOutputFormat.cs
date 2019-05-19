using System;

namespace Turkey
{
    public class TestOutputFormats
    {
        public class DotNetBunnyOutput : TestOutput
        {
            public override void AtStartup()
            {
                Console.WriteLine("\n\n(\\_/)\n(^_^)\n@(\")(\")\n\n".Replace("\n", Environment.NewLine));
            }

            public override void AfterParsingTest(string name, bool enabled)
            {
                if (enabled)
                {
                    Console.WriteLine($"Running {name}");
                }
            }

            public override void AfterRunningTest(string name, TestStatus result)
            {
                string resultText;
                switch (result)
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
            }

            public override void AfterRunningAllTests(TestResults results)
            {
                Console.WriteLine($"Total: {results.Total} Passed: {results.Passed} Failed: {results.Failed}");
            }
        }

        public class NewOutput : TestOutput
        {
            public override void AfterParsingTest(string name, bool enabled)
            {
                var nameText = string.Format("{0,-60}", name);
                Console.WriteLine(nameText);
            }

            public override void AfterRunningTest(string name, TestStatus result)
            {
                string resultOutput = null;
                if (Console.IsOutputRedirected || Console.IsErrorRedirected)
                {
                    switch (result)
                    {
                        case TestStatus.Passed: resultOutput = "PASS"; break;
                        case TestStatus.Failed: resultOutput = "FAIL"; break;
                        case TestStatus.Skipped: resultOutput = "SKIP"; break;
                    }
                    Console.WriteLine($"[{resultOutput}]");
                }
                else
                {
                    switch (result)
                    {
                        case TestStatus.Passed: resultOutput = "\u001b[32mPASS\u001b[0m"; break;
                        case TestStatus.Failed: resultOutput = "\u001b[31mFAIL\u001b[0m"; break;
                        case TestStatus.Skipped: resultOutput = "SKIP"; break;
                    }
                    Console.WriteLine($"[{resultOutput}]");
                }
            }

            public override void AfterRunningAllTests(TestResults results)
            {
                Console.WriteLine($"Total: {results.Total} Passed: {results.Passed} Failed: {results.Failed}");
            }
        }
    }
}
