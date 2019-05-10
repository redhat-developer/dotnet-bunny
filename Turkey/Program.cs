using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;

namespace Turkey
{
    class Program
    {
        public static readonly Option verboseOption = new Option(
            new string[] { "--verbose", "-v" },
            "Show verbose output", new Argument<bool>());

        public static readonly Option logDirectoryOption = new Option(
            new string[] { "--log-directory", "-l" },
            "Set directory for writing log files", new Argument<string>());

        public static async Task<int> Run(bool verbose, string logDir)
        {
            var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

            DirectoryInfo logDirectory;
            if (string.IsNullOrEmpty(logDir))
            {
                logDirectory = currentDirectory;
            }
            else
            {
                logDirectory = new DirectoryInfo(logDir);
            }

            DotNet dotnet = new DotNet();

            SystemUnderTest system = new SystemUnderTest(
                runtimeVersion: dotnet.LatestRuntimeVersion,
                sdkVersion: dotnet.LatestSdkVersion,
                platformIds: new PlatformId().CurrentIds
            );

            TestRunner runner = new TestRunner(
                system: system,
                root: currentDirectory,
                verboseOutput: verbose,
                logDirectory: logDirectory);

            var results = await runner.ScanAndRunAsync();

            Console.WriteLine($"Total: {results.Total} Passed: {results.Passed} Failed: {results.Failed}");

            int exitCode = (results.Total == results.Passed) ? 0 : 1;
            return exitCode;
        }

        static async Task<int> Main(string[] args)
        {
            Func<bool, string, Task<int>> action = Run;
            var rootCommand = new RootCommand(description: "A test runner for running standalone bash-based or xunit tests",
                                              handler: CommandHandler.Create(action));

            var parser = new CommandLineBuilder(rootCommand)
                .AddOption(logDirectoryOption)
                .AddOption(verboseOption)
                .UseVersionOption()
                .UseHelp()
                .UseParseErrorReporting()
                .Build();
            return await parser.InvokeAsync(args);
        }
    }
}
