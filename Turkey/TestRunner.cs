using System;
using System.Collections.Generic;
using System.IO;
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
        private LogWriter logWriter;
        private Cleaner cleaner;

        public TestRunner(SystemUnderTest system, DirectoryInfo root, bool verboseOutput, LogWriter logWriter, Cleaner cleaner)
        {
            this.root = root;
            this.system = system;
            this.verboseOutput = verboseOutput;
            this.logWriter = logWriter;
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
                await cleaner.CleanLocalDotNetCacheAsync();

                var parsedTest = await parser.TryParseAsync(system, file);
                if (!parsedTest.Success)
                {
                    Console.WriteLine($"WARNING: Unable to parse {file}");
                    continue;
                }

                var test = parsedTest.Test;

                if (test.Descriptor.Cleanup)
                {
                    await cleaner.CleanProjectLocalDotNetCruft();
                }

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
                    await logWriter.WriteAsync(test.Descriptor.Name, result.StandardOutput, result.StandardError);
                }

                afterEachTest.Invoke(test.Descriptor.Name, result.Status);
            }

            return results;
        }
    }
}
