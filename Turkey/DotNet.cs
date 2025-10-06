using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Turkey
{
    public class DotNet
    {
        private string _dotnetPath;

        public DotNet()
        {
            _dotnetPath = FindProgramInPath("dotnet");
            if (_dotnetPath is not null)
            {
                // resolve link target.
                _dotnetPath = new FileInfo(_dotnetPath).ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? _dotnetPath;
            }
        }

        private string DotnetFileName => _dotnetPath ?? throw new FileNotFoundException("dotnet");

        private string DotnetRoot => Path.GetDirectoryName(DotnetFileName);

        public List<Version> RuntimeVersions
        {
            get
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = DotnetFileName,
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
                        .Where(line => line.StartsWith("Microsoft.NETCore.App", StringComparison.Ordinal))
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

        public bool IsCoreClrRuntime(Version runtimeVersion)
            => !IsMonoRuntime(runtimeVersion);

        public bool IsMonoRuntime(Version runtimeVersion)
            => IsMonoRuntime(DotnetRoot, runtimeVersion);

        public List<Version> SdkVersions
        {
            get
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        FileName = DotnetFileName,
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

        public Task<int> BuildAsync(DirectoryInfo workingDirectory, IReadOnlyDictionary<string, string> environment, Action<string> logger, CancellationToken token)
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

        public Task<int> RunAsync(DirectoryInfo workingDirectory, IReadOnlyDictionary<string, string> environment, Action<string> logger, CancellationToken token)
            => RunDotNetCommandAsync(workingDirectory, new string[] { "run", "--no-restore", "--no-build"} , environment, logger, token);

        public Task<int> TestAsync(DirectoryInfo workingDirectory, IReadOnlyDictionary<string, string> environment, Action<string> logger, CancellationToken token)
            => RunDotNetCommandAsync(workingDirectory, new string[] { "test", "--no-restore", "--no-build"} , environment, logger, token);

        private async Task<int> RunDotNetCommandAsync(DirectoryInfo workingDirectory, string[] commands, IReadOnlyDictionary<string, string> environment, Action<string> logger, CancellationToken token)
        {
            var arguments = string.Join(" ", commands);
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = DotnetFileName,
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

            return await ProcessRunner.RunAsync(startInfo, logger, token).ConfigureAwait(false);
        }

        private static bool IsMonoRuntime(string dotnetRoot, Version version)
        {
            var libcoreclrPath = Path.Combine(dotnetRoot, "shared", "Microsoft.NETCore.App", version.ToString(), "libcoreclr.so");

            if (!File.Exists(libcoreclrPath))
            {
                throw new FileNotFoundException($"libcoreclr.so not found at {libcoreclrPath}");
            }

            var monoMarkers = new[]
            {
                "mono_jit_init",
                "mono_class_get",
                "mono_runtime_class_init",
                "mono_thread_set_main"
            };

            byte[] fileBytes = File.ReadAllBytes(libcoreclrPath);
            ReadOnlySpan<byte> fileSpan = fileBytes.AsSpan();
            foreach (var marker in monoMarkers)
            {
                byte[] markerBytes = Encoding.ASCII.GetBytes(marker);

                if (fileSpan.IndexOf(markerBytes) != -1)
                {
                    return true;
                }
            }

            return false;
        }

        #nullable enable
        private static string? FindProgramInPath(string program)
        #nullable disable
        {
            string[] paths = Environment.GetEnvironmentVariable("PATH")?.Split(':', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            foreach (string p in paths)
            {
                if (Path.Combine(p, program) is var filename && File.Exists(filename))
                {
                    return filename;
                }
            }
            return null;
        }
    }
}
