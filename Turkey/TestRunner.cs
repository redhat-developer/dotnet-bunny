using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Turkey
{
    public class TestResults
    {
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public int Total { get; set; }
    }

    public class TestRunner
    {

        private DirectoryInfo root;
        private bool verboseOutput;
        private DirectoryInfo logDirectory;

        public TestRunner(DirectoryInfo root, bool verboseOutput, DirectoryInfo logDirectory)
        {
            this.root = root;
            this.verboseOutput = verboseOutput;
            this.logDirectory = logDirectory;
        }

        public async Task<TestResults> ScanAndRunAsync()
        {
            TestResults results = new TestResults();

            var options = new EnumerationOptions();
            options.RecurseSubdirectories = true;

            foreach (var file in root.EnumerateFiles("test.json", options))
            {
                Test test = await TestParser.ParseAsync(file);
                var result = await test.RunAsync();
                results.Total++;
                switch (result.Status)
                {
                    case TestStatus.Passed: results.Passed++; break;
                    case TestStatus.Failed: results.Failed++; break;
                    case TestStatus.Skipped: results.Skipped++; break;
                }

                if (result.Status == TestStatus.Failed)
                {
                    var logFileName = $"logfile-{test.Name}.log";
                    var path = Path.Combine(logDirectory.FullName, logFileName);
                    using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
                    {
                        await sw.WriteAsync("# Standard Output:" + Environment.NewLine);
                        await sw.WriteAsync(result.StandardOutput);
                        await sw.WriteAsync("# Standard Error:" + Environment.NewLine);
                        await sw.WriteAsync(result.StandardError);
                    }
                }

                if (Console.IsOutputRedirected || Console.IsErrorRedirected)
                {
                    string resultOutput = null;
                    switch (result.Status)
                    {
                        case TestStatus.Passed: resultOutput = "PASS"; break;
                        case TestStatus.Failed: resultOutput = "FAIL"; break;
                        case TestStatus.Skipped: resultOutput = "SKIP"; break;
                    }
                    Console.WriteLine($"[{resultOutput}] {test.Name}");
                }
                else
                {
                    string resultOutput = null;
                    switch (result.Status)
                    {
                        case TestStatus.Passed: resultOutput = "\u001b[32mPASS\u001b[0m"; break;
                        case TestStatus.Failed: resultOutput = "\u001b[31mFAIL\u001b[0m"; break;
                        case TestStatus.Skipped: resultOutput = "SKIP"; break;
                    }
                    Console.WriteLine($"[{resultOutput}] {test.Name}");
                }
            }
            return results;
        }
    }
}
