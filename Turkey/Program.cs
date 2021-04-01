using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;

namespace Turkey
{
    public class Program
    {
        public static readonly Option<bool> verboseOption = new Option<bool>(
            new string[] { "--verbose", "-v" },
            "Show verbose output");

        public static readonly Option<bool> compatibleOption = new Option<bool>(
            new string[] { "--compatible", "-c" },
            "Make output compatible with dotnet-bunny");

        public static readonly Option<string> logDirectoryOption = new Option<string>(
            new string[] { "--log-directory", "-l" },
            "Set directory for writing log files");

        public static readonly Option<string> additionalFeedOption = new Option<string>(
            new string[] { "--additional-feed", "-s" },
            "Additional nuget repository feed");

        public static readonly Option<int> timeoutOption = new Option<int>(
            new string[] { "--timeout", "-t" },
            "Set the timeout duration for test in seconds");

        public static async Task<int> Run(string testRoot,
                                          bool verbose,
                                          bool compatible,
                                          string logDirectory,
                                          string additionalFeed,
                                          int timeout)
        {
            TimeSpan timeoutForEachTest;
            if (timeout == 0)
            {
                timeoutForEachTest = new TimeSpan(hours: 0, minutes: 5, seconds: 0);
            }
            else
            {
                timeoutForEachTest = new TimeSpan(hours: 0, minutes: 0, seconds: timeout);
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

            DirectoryInfo testRootDirectory;
            if (string.IsNullOrEmpty(testRoot))
            {
                testRootDirectory = currentDirectory;
            }
            else
            {
                testRootDirectory = new DirectoryInfo(testRoot);
                if (!testRootDirectory.Exists)
                {
                    Console.WriteLine($"error: Test root '{testRootDirectory.FullName}' does not exist");
                    return 1;
                }
            }
            Console.WriteLine($"Testing everything under {testRootDirectory.FullName}");

            LogWriter logWriter = new LogWriter(logDir);

            Cleaner cleaner = new Cleaner();

            DotNet dotnet = new DotNet();

            TestOutput defaultOutput = new TestOutputFormats.NewOutput(logWriter);
            if (compatible)
            {
                defaultOutput = new TestOutputFormats.DotNetBunnyOutput(logWriter);
            }

            TestOutput junitOutput = new TestOutputFormats.JUnitOutput(logDir);

            var testOutputs = new List<TestOutput>() {
                defaultOutput,
                junitOutput,
            };

            List<string> platformIds = new PlatformId().CurrentIds;
            Console.WriteLine($"Current platform is: {string.Join(", ", platformIds)}");

            SystemUnderTest system = new SystemUnderTest(
                runtimeVersion: dotnet.LatestRuntimeVersion,
                sdkVersion: dotnet.LatestSdkVersion,
                platformIds: platformIds
            );

            Version packageVersion = dotnet.LatestRuntimeVersion;
            string nuGetConfig = await GenerateNuGetConfigIfNeededAsync(additionalFeed, packageVersion);

            TestRunner runner = new TestRunner(
                cleaner: cleaner,
                system: system,
                root: testRootDirectory,
                verboseOutput: verbose,
                nuGetConfig: nuGetConfig);

            var cancellationTokenSource = new Func<CancellationTokenSource>(() => new CancellationTokenSource(timeoutForEachTest));

            var results = await runner.ScanAndRunAsync(testOutputs, cancellationTokenSource);

            int exitCode = (results.Failed == 0) ? 0 : 1;
            return exitCode;
        }

        public static async Task<string> GenerateNuGetConfigIfNeededAsync(string additionalFeed, Version netCoreAppVersion)
        {
            var urls = new List<string>();

            if (!string.IsNullOrEmpty(additionalFeed))
            {
                urls.Add(additionalFeed);
            }

            using (HttpClient client = new HttpClient())
            {
                var nuget = new NuGet(client);
                var sourceBuild = new SourceBuild(client);
                try
                {
                    var prodConUrl = await GetProdConFeedUrlIfNeededAsync(nuget, sourceBuild, netCoreAppVersion);
                    if( !string.IsNullOrEmpty(prodConUrl) )
                    {
                        prodConUrl = prodConUrl.Trim();
                        Console.WriteLine($"Packages are not live on nuget.org; using {prodConUrl} as additional package source");
                        urls.Add(prodConUrl);
                    }
                }
                catch( HttpRequestException exception )
                {
                    Console.WriteLine("WARNING: failed to get ProdCon url. Ignoring Exception:");
                    Console.WriteLine(exception.Message);
                    Console.WriteLine(exception.StackTrace);
                }

                string nugetConfig = null;
                try
                {
                    nugetConfig = await sourceBuild.GetNuGetConfigAsync(netCoreAppVersion);
                }
                catch( HttpRequestException exception )
                {
                    Console.WriteLine("WARNING: failed to get NuGet.config from source-build. Ignoring Exception:");
                    Console.WriteLine(exception.Message);
                    Console.WriteLine(exception.StackTrace);
                }

                return await nuget.GenerateNuGetConfig(urls, nugetConfig);
            }

            return null;
        }

        public static async Task<string> GetProdConFeedUrlIfNeededAsync(NuGet nuget, SourceBuild sourceBuild, Version netCoreAppVersion)
        {
            bool live = await nuget.IsPackageLiveAsync("runtime.linux-x64.Microsoft.NetCore.DotNetAppHost", netCoreAppVersion);
            if (!live)
            {
                return await sourceBuild.GetProdConFeedAsync(netCoreAppVersion);
            }

            return null;
        }

        static async Task<int> Main(string[] args)
        {
            Func<string, bool, bool, string, string, int, Task<int>> action = Run;
            var rootCommand = new RootCommand(description: "A test runner for running standalone bash-based or xunit tests");
            rootCommand.Handler = CommandHandler.Create(action);

            var testRootArgument = new Argument<string>();
            testRootArgument.Name = "testRoot";
            testRootArgument.Description = "Root directory for searching for tests";
            testRootArgument.Arity = ArgumentArity.ZeroOrOne;

            rootCommand.AddArgument(testRootArgument);
            rootCommand.AddOption(compatibleOption);
            rootCommand.AddOption(verboseOption);
            rootCommand.AddOption(logDirectoryOption);
            rootCommand.AddOption(additionalFeedOption);
            rootCommand.AddOption(timeoutOption);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
