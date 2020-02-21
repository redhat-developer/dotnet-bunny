using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Turkey
{

    public class XUnitTest : Test
    {
        public XUnitTest(DirectoryInfo directory, SystemUnderTest system, string nuGetConfig, TestDescriptor descriptor, bool enabled)
            : base(directory, system, nuGetConfig, descriptor, enabled)
        {
        }

        protected override async Task<TestResult> InternalRunAsync(CancellationToken cancellationToken)
        {
            PartialResult result;
            string stdout = "";
            string stderr = "";
            try
            {
                result = await BuildProjectAsync(cancellationToken);
                stdout += Environment.NewLine + result.StandardOutput;
                stderr += Environment.NewLine + result.StandardError;
                if (!result.Success)
                {
                    return new TestResult(TestStatus.Failed, stdout, stderr);
                }

                result = await TestProjectAsync(cancellationToken);
                stdout += Environment.NewLine + result.StandardOutput;
                stderr += Environment.NewLine + result.StandardError;
            }
            catch (OperationCanceledException)
            {
                stdout += Environment.NewLine + "[[TIMEOUT]]" + Environment.NewLine;
                stderr += Environment.NewLine + "[[TIMEOUT]]" + Environment.NewLine;
                result.Success = false;
            }

            return new TestResult(result.Success ? TestStatus.Passed : TestStatus.Failed,
                                  stdout,
                                  stderr);
        }


        private async Task<PartialResult> BuildProjectAsync(CancellationToken token)
        {
            return ProcessResultToPartialResult(await DotNet.BuildAsync(Directory, token));
        }

        private async Task<PartialResult> TestProjectAsync(CancellationToken token)
        {
            return ProcessResultToPartialResult(await DotNet.TestAsync(Directory, token));
        }

        private static PartialResult ProcessResultToPartialResult(DotNet.ProcessResult result)
        {
            return new PartialResult(result.ExitCode == 0, result.StandardOutput, result.StandardError);
        }
    }
}
