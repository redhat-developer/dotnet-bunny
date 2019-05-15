using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Turkey
{
    public class BashTest : Test
    {
        public BashTest(DirectoryInfo directory, SystemUnderTest system, TestDescriptor test, bool enabled)
            : base(directory, system, test, enabled)
        {
        }

        protected override async Task<TestResult> InternalRunAsync()
        {
            FileInfo testFile = new FileInfo(Path.Combine(Directory.FullName, "test.sh"));
            if (!testFile.Exists)
            {
                throw new Exception();
            }
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = testFile.FullName,
                WorkingDirectory = Directory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            Process p = Process.Start(startInfo);
            p.WaitForExit();
            // TODO timeout + kill
            var status = (p.ExitCode == 0) ? TestStatus.Passed: TestStatus.Failed;
            return new TestResult(
                status: status,
                standardOutput: p.StandardOutput.ReadToEnd(),
                standardError: p.StandardError.ReadToEnd());
        }
    }
}
