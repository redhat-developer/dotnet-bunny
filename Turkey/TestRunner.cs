using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Turkey
{
    public class TestResults
    {
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public int Total { get; set; }
    }

    public class SystemUnderTest
    {
        public Version RuntimeVersion { get; }
        public Version SdkVersion { get; }
        public List<string> CurrentPlatformIds { get; }

        public SystemUnderTest(Version runtimeVersion, Version sdkVersion, List<string> platformIds)
        {
            RuntimeVersion = runtimeVersion;
            SdkVersion = sdkVersion;
            CurrentPlatformIds = platformIds;
        }
    }

    public class TestRunner
    {
        private SystemUnderTest system;
        private DirectoryInfo root;
        private bool verboseOutput;
        private DirectoryInfo logDirectory;
        private Cleaner cleaner;

        public TestRunner(SystemUnderTest system, DirectoryInfo root, bool verboseOutput, DirectoryInfo logDirectory, Cleaner cleaner)
        {
            this.root = root;
            this.system = system;
            this.verboseOutput = verboseOutput;
            this.logDirectory = logDirectory;
            this.cleaner = cleaner;
        }

        public async Task<TestResults> ScanAndRunAsync(Action<string, TestStatus> afterEachTest)
        {
            TestResults results = new TestResults();

            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
            };

            TestParser parser = new TestParser();

            // sort tests before running to keep test order the same everywhere
            var sortedFiles = root
                .EnumerateFiles("test.json", options)
                .OrderBy(f => f.DirectoryName);

            foreach (var file in sortedFiles)
            {
                cleaner.CleanLocalDotNetCache();

                var parsedTest = await parser.TryParseAsync(system, file);
                if (!parsedTest.Success)
                {
                    Console.WriteLine($"WARNING: Unable to parse {file}");
                    continue;
                }

                var test = parsedTest.Test;
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
                    var logFileName = $"logfile-{test.Descriptor.Name}.log";
                    var path = Path.Combine(logDirectory.FullName, logFileName);
                    using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
                    {
                        await sw.WriteAsync("# Standard Output:" + Environment.NewLine);
                        await sw.WriteAsync(result.StandardOutput);
                        await sw.WriteAsync("# Standard Error:" + Environment.NewLine);
                        await sw.WriteAsync(result.StandardError);
                    }
                }

                afterEachTest.Invoke(test.Descriptor.Name, result.Status);
            }

            return results;
        }
    }
}
