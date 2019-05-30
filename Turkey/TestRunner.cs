using System;
using System.Collections.Generic;
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
        public virtual void AtStartup() {}
        public virtual void BeforeTest() {}
        public virtual void AfterParsingTest(string name, bool enabled) {}
        public virtual void AfterRunningTest(string name, TestStatus result) {}
        public virtual void AfterRunningAllTests(TestResults results) {}
    }

    public class TestRunner
    {
        private SystemUnderTest system;
        private DirectoryInfo root;
        private bool verboseOutput;
        private LogWriter logWriter;
        private Cleaner cleaner;
        private string nuGetConfig;

        public TestRunner(SystemUnderTest system, DirectoryInfo root, bool verboseOutput, LogWriter logWriter, Cleaner cleaner, string nuGetConfig)
        {
            this.root = root;
            this.system = system;
            this.verboseOutput = verboseOutput;
            this.logWriter = logWriter;
            this.cleaner = cleaner;
            this.nuGetConfig = nuGetConfig;
        }

        public async Task<TestResults> ScanAndRunAsync(TestOutput output, Func<CancellationTokenSource> GetNewCancellationToken)
        {
            output.AtStartup();

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
                var cancellationTokenSource = GetNewCancellationToken();
                var cancellationToken = cancellationTokenSource.Token;
                await cleaner.CleanLocalDotNetCacheAsync();

                var parsedTest = await parser.TryParseAsync(system, nuGetConfig, file);
                if (!parsedTest.Success)
                {
                    Console.WriteLine($"WARNING: Unable to parse {file}");
                    continue;
                }

                var test = parsedTest.Test;

                output.AfterParsingTest(test.Descriptor.Name, !test.Skip);

                if (test.Descriptor.Cleanup)
                {
                    await cleaner.CleanProjectLocalDotNetCruftAsync();
                }

                var result = await test.RunAsync(cancellationToken);
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

                output.AfterRunningTest(test.Descriptor.Name, result.Status);
            }

            output.AfterRunningAllTests(results);

            return results;
        }
    }
}
