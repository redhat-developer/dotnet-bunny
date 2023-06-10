using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Turkey
{
    public class DotNet
    {
        public List<Version> RuntimeVersions
        {
            get
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "dotnet",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Arguments = "--list-runtimes",
                };
                using (Process p = Process.Start(startInfo))
                {
                    p.WaitForExit();
                    string output = p.StandardOutput.ReadToEnd();
                    var list = output
                        .Split("\n", StringSplitOptions.RemoveEmptyEntries)
                        .Where(line => line.StartsWith("Microsoft.NETCore.App"))
                        .Select(line => line.Split(" ")[1])
                        .Select(versionString => Version.Parse(versionString))
                        .OrderBy(x => x)
                        .ToList();
                    return list;
                }
            }
        }

        public Version LatestRuntimeVersion
        {
            get
            {
                return RuntimeVersions.Last();
            }
        }

        public List<Version> SdkVersions
        {
            get
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        FileName = "dotnet",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Arguments = "--list-sdks",
                    };
                using (Process p = Process.Start(startInfo))
                {
                    p.WaitForExit();
                    string output = p.StandardOutput.ReadToEnd();
                    var list = output
                        .Split("\n", StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Split(" ")[0])
                        .Select(versionString => Version.Parse(versionString))
                        .OrderBy(x => x)
                        .ToList();
                    return list;
                }
            }
        }

        public Version LatestSdkVersion
        {
            get
            {
                return SdkVersions.LastOrDefault();
            }
        }

        public static Task<int> BuildAsync(DirectoryInfo workingDirectory, IReadOnlyDictionary<string, string> environment, Action<string> logger, CancellationToken token)
        {
            var arguments = new string[]
            {
                "build",
                "-p:UseRazorBuildServer=false",
                "-p:UseSharedCompilation=false",
                "-m:1",
            };
            return RunDotNetCommandAsync(workingDirectory, arguments, environment, logger, token);
        }

        public static Task<int> RunAsync(DirectoryInfo workingDirectory, IReadOnlyDictionary<string, string> environment, Action<string> logger, CancellationToken token)
            => RunDotNetCommandAsync(workingDirectory, new string[] { "run", "--no-restore", "--no-build"} , environment, logger, token);

        public static Task<int> TestAsync(DirectoryInfo workingDirectory, IReadOnlyDictionary<string, string> environment, Action<string> logger, CancellationToken token)
            => RunDotNetCommandAsync(workingDirectory, new string[] { "test", "--no-restore", "--no-build"} , environment, logger, token);

        private static async Task<int> RunDotNetCommandAsync(DirectoryInfo workingDirectory, string[] commands, IReadOnlyDictionary<string, string> environment, Action<string> logger, CancellationToken token)
        {
            var arguments = string.Join(" ", commands);
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = workingDirectory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            startInfo.EnvironmentVariables.Clear();
            foreach (var (key, value) in environment)
            {
                startInfo.EnvironmentVariables.Add(key, value);
            }

            return await ProcessRunner.RunAsync(startInfo, logger, token);
        }
    }
}
