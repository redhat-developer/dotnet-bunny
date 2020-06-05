using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Turkey
{
    public class TestDescriptor
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public bool RequiresSdk { get; set; }
        public string Version { get; set; }
        public bool VersionSpecific { get; set; }
        public string Type { get; set; }
        public bool Cleanup { get; set; }
        public List<string> PlatformBlacklist { get; set; }
    }

    // TODO is this a strongly-typed enum in C#?
    public enum TestStatus {
        Passed, Failed, Skipped,
    }

    public class TestResult
    {
        public TestStatus Status { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }

        public TestResult(TestStatus status, string standardOutput, string standardError)
        {
            Status = status;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }
    }

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

    public abstract class Test
    {
        public DirectoryInfo Directory { get; }
        public SystemUnderTest SystemUnderTest { get; }
        public string NuGetConfig { get; }
        public TestDescriptor Descriptor { get; }
        public bool Skip { get; }

        public Test(DirectoryInfo testDirectory, SystemUnderTest system, string nuGetConfig, TestDescriptor descriptor, bool enabled)
        {
            this.Directory = testDirectory;
            this.SystemUnderTest = system;
            this.NuGetConfig = nuGetConfig;
            this.Descriptor = descriptor;
            this.Skip = !enabled;
        }

        public async Task<TestResult> RunAsync(CancellationToken cancelltionToken)
        {
            if (Skip)
            {
                return new TestResult(
                    status: TestStatus.Skipped,
                    standardOutput: null,
                    standardError: null);
            }

            var path = Path.Combine(Directory.FullName, "nuget.config");
            if (!string.IsNullOrEmpty(NuGetConfig))
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"WARNING: overwriting {path}");
                }
                await File.WriteAllTextAsync(path, NuGetConfig);
            }

            var result = await UpdateProjectFilesIfPresent();
            var stdout = result.StandardOutput;
            var stderr = result.StandardError;
            if (!result.Success)
            {
                return new TestResult(TestStatus.Failed, stdout, stderr);
            }

            var testResult = await InternalRunAsync(cancelltionToken);

            if (!string.IsNullOrEmpty(NuGetConfig))
            {
                File.Delete(path);
            }

            return testResult;
        }

        private async Task<PartialResult> UpdateProjectFilesIfPresent()
        {
            if (SystemUnderTest.RuntimeVersion < Version.Parse("2.0"))
            {
                var projectJsonPath = Path.Combine(this.Directory.FullName, "project.json");
                if (File.Exists(projectJsonPath))
                {
                    return await CopyProjectJsonFile();
                }
            }
            else
            {
                var csprojFile = $"{Directory.Name}.csproj";
                var csprojPath = Path.Combine(this.Directory.FullName, csprojFile);
                if (File.Exists(csprojPath))
                {
                    return await UpdateCsprojVersion(csprojPath);
                }
            }
            return new PartialResult(true, "No project file to update", "");
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

        private async Task<PartialResult> UpdateCsprojVersion(string csprojPath)
        {
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

        protected abstract Task<TestResult> InternalRunAsync(CancellationToken cancellationToken);

    }
}
