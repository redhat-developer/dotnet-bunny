using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Threading;

namespace Turkey
{
    public class Program
    {
        public static readonly Option verboseOption = new Option(
            new string[] { "--verbose", "-v" },
            "Show verbose output", new Argument<bool>());

        public static readonly Option compatibleOption = new Option(
            new string[] { "--compatible", "-c" },
            "Make output compatible with dotnet-bunny", new Argument<bool>());

        public static readonly Option logDirectoryOption = new Option(
            new string[] { "--log-directory", "-l" },
            "Set directory for writing log files", new Argument<string>());

        public static readonly Option timeoutOption = new Option(
            new string[] { "--timeout", "-t" },
            "Set the timeout duration for test in seconds", new Argument<int>());

        public static async Task<int> Run(bool verbose, bool compatible, string logDirectory, int timeout)
        {
            if (timeout == 0)
            {
                timeout = 60;
            }

            var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

            DirectoryInfo logDir;
            if (string.IsNullOrEmpty(logDirectory))
            {
                logDir = currentDirectory;
            }
            else
            {
                logDir = new DirectoryInfo(logDirectory);
            }

            LogWriter logWriter = new LogWriter(logDir);

            Cleaner cleaner = new Cleaner();

            DotNet dotnet = new DotNet();

            TestOutput outputFormat = new TestOutputFormats.NewOutput(logWriter);
            if (compatible)
            {
                outputFormat = new TestOutputFormats.DotNetBunnyOutput(logWriter);
            }

            SystemUnderTest system = new SystemUnderTest(
                runtimeVersion: dotnet.LatestRuntimeVersion,
                sdkVersion: dotnet.LatestSdkVersion,
                platformIds: new PlatformId().CurrentIds
            );

            Version packageVersion = dotnet.LatestRuntimeVersion;
            string nuGetConfig = await GetNuGetConfigIfNeeded(packageVersion);

            TestRunner runner = new TestRunner(
                cleaner: cleaner,
                system: system,
                root: currentDirectory,
                verboseOutput: verbose,
                nuGetConfig: nuGetConfig);

            var timeoutPerTest = new TimeSpan(hours: 0, minutes: 0, seconds: timeout);
            var cancellationTokenSource = new Func<CancellationTokenSource>(() => new CancellationTokenSource(timeoutPerTest));
            var results = await runner.ScanAndRunAsync(outputFormat, cancellationTokenSource);

            int exitCode = (results.Failed == 0) ? 0 : 1;
            return exitCode;
        }

        public static Task<string> GetNuGetConfigIfNeeded(Version netCoreAppVersion)
        {
            using (HttpClient client = new HttpClient())
            {
                var nuget = new NuGet(client);
                var sourceBuild = new SourceBuild(client);
                return GetNuGetConfigIfNeeded(nuget, sourceBuild, netCoreAppVersion);
            }
        }

        public static async Task<string> GetNuGetConfigIfNeeded(NuGet nuget, SourceBuild sourceBuild, Version netCoreAppVersion)
        {
            string nuGetConfig = null;
            bool live = await nuget.IsPackageLiveAsync("Microsoft.NetCore.App", netCoreAppVersion);
            if (!live)
            {
                var feed = await sourceBuild.GetProdConFeedAsync(netCoreAppVersion);
                if (!string.IsNullOrEmpty(feed))
                {
                    nuGetConfig = nuget.GenerateNuGetConfig(new List<string>{feed});
                    Console.WriteLine($"Packages are not live on nuget.org; using {feed} as additional package source");
                }
            }

            return nuGetConfig;
        }

        static async Task<int> Main(string[] args)
        {
            Func<bool, bool, string, int, Task<int>> action = Run;
            var rootCommand = new RootCommand(description: "A test runner for running standalone bash-based or xunit tests",
                                              handler: CommandHandler.Create(action));

            var parser = new CommandLineBuilder(rootCommand)
                .AddOption(compatibleOption)
                .AddOption(verboseOption)
                .AddOption(logDirectoryOption)
                .AddOption(timeoutOption)
                .UseVersionOption()
                .UseHelp()
                .UseParseErrorReporting()
                .Build();
            return await parser.InvokeAsync(args);
        }
    }
}
