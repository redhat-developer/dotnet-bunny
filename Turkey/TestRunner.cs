using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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

    public class TestOutput
    {
        public async virtual Task AtStartupAsync() {}
        public async virtual Task BeforeTestAsync() {}
        public async virtual Task AfterParsingTestAsync(string name, bool enabled) {}
        public async virtual Task AfterRunningTestAsync(string name, TestResult result, TimeSpan testTime) {}
        public async virtual Task AfterRunningAllTestsAsync(TestResults results) {}
    }

    public class TestRunner
    {
        private SystemUnderTest system;
        private DirectoryInfo root;
        private bool verboseOutput;
        private Cleaner cleaner;
        private string nuGetConfig;

        public TestRunner(SystemUnderTest system, DirectoryInfo root, bool verboseOutput, Cleaner cleaner, string nuGetConfig)
        {
            this.root = root;
            this.system = system;
            this.verboseOutput = verboseOutput;
            this.cleaner = cleaner;
            this.nuGetConfig = nuGetConfig;
        }

        public async Task<TestResults> ScanAndRunAsync(List<TestOutput> outputs, Func<CancellationTokenSource> GetNewCancellationToken)
        {
            await outputs.ForEachAsync(output => output.AtStartupAsync());

            TestResults results = new TestResults();

            Stopwatch testTimeWatch = new Stopwatch();

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
                testTimeWatch.Reset();
                var cancellationTokenSource = GetNewCancellationToken();
                var cancellationToken = cancellationTokenSource.Token;
                await cleaner.CleanLocalDotNetCacheAsync();

                testTimeWatch.Start();
                var parsedTest = await parser.TryParseAsync(system, nuGetConfig, file);
                if (!parsedTest.Success)
                {
                    Console.WriteLine($"WARNING: Unable to parse {file}");
                    continue;
                }

                var test = parsedTest.Test;

                await outputs.ForEachAsync(output => output.AfterParsingTestAsync(test.Descriptor.Name, !test.Skip));

                if (test.Descriptor.Cleanup)
                {
                    await cleaner.CleanProjectLocalDotNetCruftAsync();
                }

                var result = await test.RunAsync(cancellationToken);

                testTimeWatch.Stop();

                results.Total++;
                switch (result.Status)
                {
                    case TestStatus.Passed: results.Passed++; break;
                    case TestStatus.Failed: results.Failed++; break;
                    case TestStatus.Skipped: results.Skipped++; break;
                }

                await outputs.ForEachAsync(output => output.AfterRunningTestAsync(test.Descriptor.Name, result, testTimeWatch.Elapsed));
            }

            await outputs.ForEachAsync(output => output.AfterRunningAllTestsAsync(results));

            return results;
        }
    }
}
