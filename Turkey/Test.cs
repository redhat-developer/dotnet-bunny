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
        public double TimeoutMultiplier { get; set; } = 1.0;
        public List<string> IgnoredRIDs { get; set; } = new();
        public List<string> SkipWhen { get; set; } = new();
    }

    // TODO is this a strongly-typed enum in C#?
    public enum TestResult {
        Passed, Failed, Skipped,
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

        public async Task<TestResult> RunAsync(Action<string> logger, CancellationToken cancelltionToken)
        {
            if (Skip)
            {
                return TestResult.Skipped;
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

            UpdateProjectFilesIfPresent();

            var testResult = await InternalRunAsync(logger, cancelltionToken);

            if (!string.IsNullOrEmpty(NuGetConfig))
            {
                File.Delete(path);
            }

            return testResult;
        }

        private void UpdateProjectFilesIfPresent()
        {
            if (SystemUnderTest.RuntimeVersion < Version.Parse("2.0"))
            {
                var projectJsonPath = Path.Combine(this.Directory.FullName, "project.json");
                if (File.Exists(projectJsonPath))
                {
                    CopyProjectJsonFile();
                }
            }
            else
            {
                var csprojFile = $"{Directory.Name}.csproj";
                var csprojPath = Path.Combine(this.Directory.FullName, csprojFile);
                if (File.Exists(csprojPath))
                {
                    UpdateCsprojVersion(csprojPath);
                }
            }
        }

        private void CopyProjectJsonFile()
        {
            string majorMinor = "" + SystemUnderTest.RuntimeVersion.Major + SystemUnderTest.RuntimeVersion.Minor;
            var fileName = $"resources/project{majorMinor}xunit.json";
            var resourceLocation = FindResourceFile(fileName);
            var source = resourceLocation;
            var dest = Path.Combine(this.Directory.FullName, "project.json");
            File.Copy(source, dest);
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

        private void UpdateCsprojVersion(string csprojPath)
        {
            var contents = File.ReadAllText(csprojPath);
            var updatedContents = UpdateCsprojContents(contents);

            File.WriteAllText(csprojPath, updatedContents);
        }

        private string UpdateCsprojContents(string contents) =>
            new CsprojCompatibilityPatcher().Patch(contents, this.SystemUnderTest.RuntimeVersion);

        protected abstract Task<TestResult> InternalRunAsync(Action<string> logger, CancellationToken cancellationToken);

    }
}
