using System;
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

        protected override async Task<TestResult> InternalRunAsync(CancellationToken cancellationToken)
        {
            FileInfo testFile = new FileInfo(Path.Combine(Directory.FullName, "test.sh"));
            if (!testFile.Exists)
            {
                throw new Exception();
            }
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = testFile.FullName,
                Arguments = SystemUnderTest.RuntimeVersion.ToString(),
                WorkingDirectory = Directory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            Process p = Process.Start(startInfo);
            var standardOutputWriter = new StringWriter();
            var standardErrorWriter = new StringWriter();
            var status = TestStatus.Failed;
            try
            {
                await p.WaitForExitAsync(cancellationToken, standardOutputWriter, standardErrorWriter);
                status = (p.ExitCode == 0) ? TestStatus.Passed: TestStatus.Failed;
            }
            catch (OperationCanceledException)
            {
                standardOutputWriter.WriteLine("[[TIMEOUT]]");
                standardErrorWriter.WriteLine("[[TIMEOUT]]");
            }

            return new TestResult(
                status: status,
                standardOutput: standardOutputWriter.ToString(),
                standardError: standardErrorWriter.ToString());
        }
    }
}
