using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Turkey
{
    public class BashTest : Test
    {
        public BashTest(DirectoryInfo directory, TestDescriptor test) : base(directory, test)
        {
        }

        protected override async Task<TestResult> InternalRunAsync()
        {
            FileInfo testFile = new FileInfo(Directory + "/test.sh");
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
            return new TestResult()
            {
                Status = (p.ExitCode == 0) ? TestStatus.Passed: TestStatus.Failed,
                StandardOutput = p.StandardOutput.ReadToEnd(),
                StandardError = p.StandardError.ReadToEnd(),
            };
        }
    }
}
