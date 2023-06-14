using System;
using System.Collections.Generic;
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

        protected override async Task<TestResult> InternalRunAsync(Action<string> logger, CancellationToken cancellationToken)
        {
            bool success =    await BuildProjectAsync(logger, cancellationToken) == 0
                           && await RunTestProjectAsync(logger, cancellationToken) == 0;

            return success ? TestResult.Passed : TestResult.Failed;
        }


        private Task<int> BuildProjectAsync(Action<string> logger, CancellationToken token)
            => SystemUnderTest.Dotnet.BuildAsync(Directory, SystemUnderTest.EnvironmentVariables, logger, token);

        private Task<int> RunTestProjectAsync(Action<string> logger, CancellationToken token)
            => SystemUnderTest.Dotnet.TestAsync(Directory, SystemUnderTest.EnvironmentVariables, logger, token);
    }
}
