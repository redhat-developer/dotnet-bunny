using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Runtime.InteropServices;

namespace Turkey
{
    public class Program
    {
        public static readonly Option<bool> verboseOption = new Option<bool>(
            new string[] { "--verbose", "-v" },
            "Show verbose output");

        public static readonly Option<string> logDirectoryOption = new Option<string>(
            new string[] { "--log-directory", "-l" },
            "Set directory for writing log files");

        public static readonly Option<string> additionalFeedOption = new Option<string>(
            new string[] { "--additional-feed", "-s" },
            "Additional nuget repository feed");

        public static readonly Option<int> timeoutOption = new Option<int>(
            new string[] { "--timeout", "-t" },
            "Set the timeout duration for test in seconds");

        public static readonly Option<IEnumerable<string>> traitOption = new Option<IEnumerable<string>>(
            new string[] { "--trait" },
            "Add a trait which is used to disable tests.")
        {
            Arity = ArgumentArity.ZeroOrMore
        };

        public static async Task<int> Run(string testRoot,
                                          bool verbose,
                                          string logDirectory,
                                          string additionalFeed,
                                          IEnumerable<string> trait,
                                          int timeout)
        {
            TimeSpan defaultTimeout;
            if (timeout == 0)
            {
                defaultTimeout = new TimeSpan(hours: 0, minutes: 5, seconds: 0);
            }
            else
            {
                defaultTimeout = new TimeSpan(hours: 0, minutes: 0, seconds: timeout);
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

            Cleaner cleaner = new Cleaner();

            DotNet dotnet = new DotNet();
            Version runtimeVersion = dotnet.LatestRuntimeVersion;

            TestOutput defaultOutput = new TestOutputFormats.NewOutput();

            TestOutput junitOutput = new TestOutputFormats.JUnitOutput(logDir);

            var testOutputs = new List<TestOutput>() {
                defaultOutput,
                junitOutput,
            };

            List<string> platformIds = new PlatformId().CurrentIds;
            Console.WriteLine($"Current platform is: {string.Join(", ", platformIds)}");

            var sanitizer = new EnvironmentVariableSanitizer();
            var envVars = sanitizer.SanitizeCurrentEnvironmentVariables();

            var traits = CreateTraits(runtimeVersion, dotnet.LatestSdkVersion, platformIds, dotnet.IsMonoRuntime(runtimeVersion), trait);
            Console.WriteLine($"Tests matching these traits will be skipped: {string.Join(", ", traits.OrderBy(s => s))}.");

            envVars["TestTargetFramework"] = $"net{runtimeVersion.Major}.{runtimeVersion.Minor}";

            SystemUnderTest system = new SystemUnderTest(
                dotnet,
                runtimeVersion: runtimeVersion,
                sdkVersion: dotnet.LatestSdkVersion,
                platformIds: platformIds,
                environmentVariables: envVars,
                traits: traits
            );

            Version packageVersion = runtimeVersion;
            string nuGetConfig = await GenerateNuGetConfigIfNeededAsync(additionalFeed, packageVersion,
                                                                        useSourceBuildNuGetConfig: false);
            if (verbose && nuGetConfig != null)
            {
                Console.WriteLine("Using nuget.config: ");
                Console.WriteLine(nuGetConfig);
            }

            TestRunner runner = new TestRunner(
                cleaner: cleaner,
                system: system,
                root: testRootDirectory,
                verboseOutput: verbose,
                nuGetConfig: nuGetConfig);

            var results = await runner.ScanAndRunAsync(testOutputs, logDir.FullName, defaultTimeout);

            int exitCode = (results.Failed == 0) ? 0 : 1;
            return exitCode;
        }

        public static async Task<string> GenerateNuGetConfigIfNeededAsync(string additionalFeed, Version netCoreAppVersion, bool useSourceBuildNuGetConfig)
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

                if (netCoreAppVersion.Major < 4)
                {
                    try
                    {
                        var prodConUrl = await GetProdConFeedUrlIfNeededAsync(nuget, sourceBuild, netCoreAppVersion);
                        if (!string.IsNullOrEmpty(prodConUrl))
                        {
                            prodConUrl = prodConUrl.Trim();
                            Console.WriteLine($"Packages are not live on nuget.org; using {prodConUrl} as additional package source");
                            urls.Add(prodConUrl);
                        }
                    }
                    catch (HttpRequestException exception)
                    {
                        Console.WriteLine("WARNING: failed to get ProdCon url. Ignoring Exception:");
                        Console.WriteLine(exception.ToString());
                    }
                }

                string nugetConfig = null;
                if (useSourceBuildNuGetConfig)
                {
                    try
                    {
                        nugetConfig = await sourceBuild.GetNuGetConfigAsync(netCoreAppVersion);
                    }
                    catch( HttpRequestException exception )
                    {
                        Console.WriteLine("WARNING: failed to get NuGet.config from source-build. Ignoring Exception:");
                        Console.WriteLine(exception.ToString());
                    }
                }

                if (urls.Any() || nugetConfig != null)
                {
                    // Add the default nuget repo that customer should always
                    // be using anyway. This is the default, but still useful
                    // if the nugetConfig has a <clear/> element that removes
                    // it.
                    urls.Add("https://api.nuget.org/v3/index.json");
                    return await nuget.GenerateNuGetConfig(urls, nugetConfig);
                }
            }

            return null;
        }

        public static IReadOnlySet<string> CreateTraits(Version runtimeVersion, Version sdkVersion, List<string> rids, bool isMonoRuntime, IEnumerable<string> additionalTraits)
        {
            var traits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add 'version=' traits.
            traits.Add($"version={runtimeVersion.Major}.{runtimeVersion.Minor}");
            traits.Add($"version={runtimeVersion.Major}");

            // Add 'os=', 'rid=' traits.
            foreach (var rid in rids)
            {
                traits.Add($"rid={rid}");
                if (rid.LastIndexOf('-') is int offset && offset != -1)
                {
                    traits.Add($"os={rid.Substring(0, offset)}");
                }
            }

            // Add 'arch=' trait.
            string arch = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
            traits.Add($"arch={arch}");

            // Add 'runtime=' trait.
            traits.Add($"runtime={(isMonoRuntime ? "mono" : "coreclr")}");

            // Add additional traits.
            foreach (var skipTrait in additionalTraits)
            {
                traits.Add(skipTrait);
            }

            return traits;
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
            var rootCommand = new RootCommand(description: "A test runner for running standalone bash-based or xunit tests");
            rootCommand.Handler = CommandHandler.Create(Run);

            var testRootArgument = new Argument<string>();
            testRootArgument.Name = "testRoot";
            testRootArgument.Description = "Root directory for searching for tests";
            testRootArgument.Arity = ArgumentArity.ZeroOrOne;

            rootCommand.AddArgument(testRootArgument);
            rootCommand.AddOption(verboseOption);
            rootCommand.AddOption(logDirectoryOption);
            rootCommand.AddOption(additionalFeedOption);
            rootCommand.AddOption(traitOption);
            rootCommand.AddOption(timeoutOption);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
