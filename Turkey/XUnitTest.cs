using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Turkey
{
    struct PartialResult
    {
        public bool Success;
        public string StandardOutput;
        public string StandardError;

        public PartialResult(bool success, string stdout, string stderr)
        {
            Success = success;
            StandardOutput = stdout;
            StandardError = stderr;
        }
    }

    public class XUnitTest : Test
    {
        public XUnitTest(DirectoryInfo directory, SystemUnderTest system, string nuGetConfig, TestDescriptor descriptor, bool enabled)
            : base(directory, system, nuGetConfig, descriptor, enabled)
        {
        }

        protected override async Task<TestResult> InternalRunAsync(CancellationToken cancellationToken)
        {
            var result = await UpdateProjectFiles();
            var stdout = result.StandardOutput;
            var stderr = result.StandardError;
            if (!result.Success)
            {
                return new TestResult(TestStatus.Failed, stdout, stderr);
            }

            try
            {
                result = await BuildProjectAsync(cancellationToken);
                stdout = stdout + Environment.NewLine + result.StandardOutput;
                stderr = stderr + Environment.NewLine + result.StandardError;
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

        private async Task<PartialResult> UpdateProjectFiles()
        {
            if (SystemUnderTest.RuntimeVersion < Version.Parse("2.0"))
            {
                return await CopyProjectJsonFile();
            }
            else
            {
                return await UpdateCsprojVersion();
            }
        }

        private async Task<PartialResult> CopyProjectJsonFile()
        {
            string majorMinor = "" + SystemUnderTest.RuntimeVersion.Major + SystemUnderTest.RuntimeVersion.Minor;
            var fileName = $"resources/project{majorMinor}xunit.json";
            var resourceLocation = FindResourceFile(fileName);
            var source = resourceLocation;
            var dest = Path.Combine(this.Directory.FullName, "project.json");
            File.Copy(source, dest);
            return new PartialResult(true, "", "");
        }

        private static string FindResourceFile(string name)
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var dir = Path.GetDirectoryName(assemblyLocation);
            var resourceLocation = Path.Combine(dir, name);
            if (!File.Exists(resourceLocation))
            {
                throw new Exception($"Resource {name} at location {resourceLocation} does not exist");
            }
            return resourceLocation;
        }

        private async Task<PartialResult> UpdateCsprojVersion()
        {
            var csprojFile = $"{Directory.Name}.csproj";
            var csprojPath = Path.Combine(this.Directory.FullName, csprojFile);
            if (!File.Exists(csprojPath))
            {
                return new PartialResult(false, "", $"error: {csprojPath} doesn't exist");
            }

            var contents = File.ReadAllText(csprojPath);
            var updatedContents = UpdateCsprojContents(contents);

            File.WriteAllText(csprojPath, updatedContents);

            return new PartialResult(true, "", "");
        }

        private string UpdateCsprojContents(string contents)
        {
            var pattern = "<TargetFramework>netcoreapp\\d\\.\\d+</TargetFramework>";
            var versionString = this.SystemUnderTest.RuntimeVersion.MajorMinor;
            var replacement = $"<TargetFramework>netcoreapp{versionString}</TargetFramework>";

            var output = Regex.Replace(contents, pattern, replacement);

            return output;
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
