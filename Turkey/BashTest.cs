using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Turkey
{
    public class BashTest : Test
    {
        private DirectoryInfo testDirectory;

        public BashTest(DirectoryInfo testDirectory)
        {
            this.testDirectory = testDirectory;
        }

        protected override async Task<TestResult> InternalRunAsync()
        {
            FileInfo testFile = new FileInfo(testDirectory + "/test.sh");
            if (!testFile.Exists)
            {
                throw new Exception();
            }
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = testFile.FullName,
                WorkingDirectory = testDirectory.FullName,
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
