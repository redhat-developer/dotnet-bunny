using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        public IReadOnlyDictionary<string, string> EnvironmentVariables;
        public IReadOnlySet<string> Traits { get; }
        public DotNet Dotnet { get; }

        public SystemUnderTest(DotNet dotnet,
                               Version runtimeVersion,
                               Version sdkVersion,
                               List<string> platformIds,
                               IReadOnlyDictionary<string, string> environmentVariables,
                               IReadOnlySet<string> traits)
        {
            Dotnet = dotnet;
            RuntimeVersion = runtimeVersion;
            SdkVersion = sdkVersion;
            CurrentPlatformIds = platformIds ?? new List<string>();
            EnvironmentVariables = environmentVariables;
            Traits = traits ?? new HashSet<string>();
        }
    }

    public class TestOutput
    {
        public async virtual Task AtStartupAsync() {}
        public async virtual Task BeforeTestAsync() {}
        public async virtual Task AfterParsingTestAsync(string name, bool enabled) {}
        public async virtual Task AfterRunningTestAsync(string name, TestResult result, StringBuilder testLog, TimeSpan testTime) {}
        public async virtual Task PrintFailedTests() {}
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

        public async Task<TestResults> ScanAndRunAsync(List<TestOutput> outputs, string logDir, TimeSpan defaultTimeout)
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
                await cleaner.CleanLocalDotNetCacheAsync();

                testTimeWatch.Start();
                var parsedTest = await parser.TryParseAsync(system, nuGetConfig, file);
                if (!parsedTest.Success)
                {
                    Console.WriteLine($"WARNING: Unable to parse {file}");
                    continue;
                }
                var test = parsedTest.Test;
                string testName = test.Descriptor.Name;

                await outputs.ForEachAsync(output => output.AfterParsingTestAsync(testName, !test.Skip));

                TimeSpan testTimeout = test.Descriptor.TimeoutMultiplier * defaultTimeout;
                using var cts = testTimeout == TimeSpan.Zero ? null : new CancellationTokenSource(testTimeout);
                var cancellationToken = cts is null ? default : cts.Token;
                Action<string> testLogger = null;

                // Log to a file.
                string logFileName = Path.Combine(logDir, $"logfile-{testName}.log");
                using var logFile = new StreamWriter(logFileName) { AutoFlush = true };
                Action<string> logToLogFile = s => logFile.WriteLine(s);
                testLogger += logToLogFile;

                // Log to a StringBuilder.
                StringBuilder testLog = new();
                Action<string> logToTestLog = s => testLog.AppendLine(s);
                testLogger += logToTestLog;

                if (test.Descriptor.Cleanup)
                {
                    await cleaner.CleanProjectLocalDotNetCruftAsync();
                }

                TestResult testResult;
                try
                {
                    testResult = await test.RunAsync(testLogger, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    testLogger("[[TIMEOUT]]");
                    testResult = TestResult.Failed;
                }

                testTimeWatch.Stop();

                results.Total++;
                switch (testResult)
                {
                    case TestResult.Passed: results.Passed++; break;
                    case TestResult.Failed: results.Failed++; break;
                    case TestResult.Skipped: results.Skipped++; break;
                }

                await outputs.ForEachAsync(output => output.AfterRunningTestAsync(testName, testResult, testLog, testTimeWatch.Elapsed));
                }
            
            await outputs.ForEachAsync(outputs => outputs.PrintFailedTests());

            await outputs.ForEachAsync(output => output.AfterRunningAllTestsAsync(results));

            return results;
        }
    }
}
