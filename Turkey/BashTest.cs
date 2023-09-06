using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Turkey
{
    public class BashTest : Test
    {
        public BashTest(DirectoryInfo directory, SystemUnderTest system, string nuGetConfig, TestDescriptor test, bool enabled)
            : base(directory, system, nuGetConfig, test, enabled)
        {
        }

        protected override async Task<TestResult> InternalRunAsync(Action<string> logger, CancellationToken cancellationToken)
        {
            FileInfo testFile = new FileInfo(Path.Combine(Directory.FullName, "test.sh"));
            if (!testFile.Exists)
            {
                logger($"Unable to find 'test.sh' in {Directory.FullName}");
                return TestResult.Failed;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = testFile.FullName,
                Arguments = SystemUnderTest.RuntimeVersion.ToString(),
                WorkingDirectory = Directory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            startInfo.EnvironmentVariables.Clear();
            foreach (var (key, value) in SystemUnderTest.EnvironmentVariables)
            {
                startInfo.EnvironmentVariables.Add(key, value);
            }

            int exitCode = await ProcessRunner.RunAsync(startInfo, logger, cancellationToken);

            return exitCode == 0 ? TestResult.Passed : TestResult.Failed;
        }
    }
}
