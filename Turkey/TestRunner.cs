using System;
using System.IO;
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
                    // TODO more portable, please
                    var path = logDirectory.ToString() + "/" + logFileName;
                    await File.WriteAllTextAsync(path, "# Standard Output:" + Environment.NewLine);
                    await File.AppendAllTextAsync(path, result.StandardOutput);
                    await File.AppendAllTextAsync(path, "# Standard Error:" + Environment.NewLine);
                    await File.AppendAllTextAsync(path, result.StandardError);
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
