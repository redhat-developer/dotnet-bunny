using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Turkey
{
    public class TestOutputFormats
    {
        public class DotNetBunnyOutput : TestOutput
        {
            private readonly LogWriter _logWriter;

            public DotNetBunnyOutput(LogWriter writer)
            {
                this._logWriter = writer;
            }

            public async override Task AtStartupAsync()
            {
                Console.WriteLine("\n\n(\\_/)\n(^_^)\n@(\")(\")\n\n".Replace("\n", Environment.NewLine));
            }

            public async override Task AfterParsingTestAsync(string name, bool enabled)
            {
                if (enabled)
                {
                    Console.WriteLine($"Running {name}");
                }
            }

            public async override Task AfterRunningTestAsync(string name, TestResult result, TimeSpan testTime)
            {
                string resultText;
                switch (result.Status)
                {
                    case TestStatus.Passed:
                        resultText = "PASS";
                        Console.WriteLine($"Result: {resultText}" + Environment.NewLine);
                        break;
                    case TestStatus.Failed:
                        resultText = "FAIL - Code: 1";
                        Console.WriteLine($"Result: {resultText}" + Environment.NewLine);
                        break;
                    default:
                        break;
                }

                if (result.Status == TestStatus.Failed)
                {
                    await _logWriter.WriteAsync(name, result.StandardOutput, result.StandardError);
                }

            }

            public async override Task AfterRunningAllTestsAsync(TestResults results)
            {
                Console.WriteLine($"Total: {results.Total} Passed: {results.Passed} Failed: {results.Failed}");
            }
        }

        public class NewOutput : TestOutput
        {

            private readonly LogWriter _logWriter;

            public NewOutput(LogWriter writer)
            {
                this._logWriter = writer;
            }

            public async override Task AfterParsingTestAsync(string name, bool enabled)
            {
                var nameText = string.Format("{0,-60}", name);
                Console.Write(nameText);
            }

            public async override Task AfterRunningTestAsync(string name, TestResult result, TimeSpan testTime)
            {
                string elapsedTime = testTime.TotalMilliseconds.ToString();
                string resultOutput = null;
                if (Console.IsOutputRedirected || Console.IsErrorRedirected)
                {
                    switch (result.Status)
                    {
                        case TestStatus.Passed: resultOutput = "PASS"; break;
                        case TestStatus.Failed: resultOutput = "FAIL"; break;
                        case TestStatus.Skipped: resultOutput = "SKIP"; break;
                    }
                    Console.WriteLine($"[{resultOutput}]");
                }
                else
                {
                    switch (result.Status)
                    {
                        case TestStatus.Passed: resultOutput = "\u001b[32mPASS\u001b[0m"; break;
                        case TestStatus.Failed: resultOutput = "\u001b[31mFAIL\u001b[0m"; break;
                        case TestStatus.Skipped: resultOutput = "SKIP"; break;
                    }
                    Console.WriteLine($"[{resultOutput}]\t({elapsedTime}ms)");
                }

                await _logWriter.WriteAsync(name, result.StandardOutput, result.StandardError);
            }

            public async override Task AfterRunningAllTestsAsync(TestResults results)
            {
                Console.WriteLine($"Total: {results.Total} Passed: {results.Passed} Failed: {results.Failed}");
            }
        }

        public class JUnitOutput : TestOutput
        {
            private struct TestCase {
                public string Name;
                public string ClassName;
                public bool Failed;
                public bool Skipped;
                public string Message;
                public string StandardOutput;
                public string StandardError;
            }

            private List<TestCase> _testCases = new List<TestCase>();
            private FileInfo _resultsFile;

            public JUnitOutput(DirectoryInfo logDirectory)
            : this(new FileInfo(Path.Combine(logDirectory.FullName, "results.xml")))
            {
            }

            public JUnitOutput(FileInfo resultsFile)
            {
                _resultsFile = resultsFile;
            }

            public async override Task AfterParsingTestAsync(string name, bool enabled)
            {

            }

            public async override Task AfterRunningTestAsync(string name, TestResult result, TimeSpan testTime)
            {
                var testCase = new TestCase();
                testCase.Name = name;
                testCase.ClassName = "TestSuite";
                testCase.Failed = (result.Status == TestStatus.Failed);
                testCase.Skipped = (result.Status == TestStatus.Skipped);
                testCase.Message = "see stdout/stderr";
                testCase.StandardOutput = result.StandardOutput;
                testCase.StandardError = result.StandardError;

                _testCases.Add(testCase);
            }

            public async override Task AfterRunningAllTestsAsync(TestResults results)
            {
                var settings = new XmlWriterSettings();
                settings.Indent = true;

                using (var writer = XmlWriter.Create(_resultsFile.FullName, settings))
                {
                    writer.WriteStartDocument();

                    writer.WriteStartElement("testsuite");

                    foreach (var testCase in _testCases)
                    {
                        writer.WriteStartElement("testcase");
                        writer.WriteAttributeString("name", testCase.Name);
                        writer.WriteAttributeString("classname", testCase.ClassName);

                        if (testCase.Skipped)
                        {
                            writer.WriteStartElement("skipped");
                            writer.WriteAttributeString("message", testCase.Message);
                            writer.WriteEndElement();
                        }

                        if (testCase.Failed)
                        {
                            writer.WriteStartElement("failure");
                            writer.WriteAttributeString("message", testCase.Message);
                            writer.WriteAttributeString("type", "AssertionError");
                            writer.WriteEndElement();
                        }

                        if (testCase.StandardOutput != null)
                        {
                            writer.WriteStartElement("system-out");
                            string standardOutput = RemoveInvalidXmlCharacters(testCase.StandardOutput);
                            writer.WriteString(standardOutput);
                            writer.WriteEndElement();
                        }

                        if (testCase.StandardError != null)
                        {
                            writer.WriteStartElement("system-err");
                            string standardError = RemoveInvalidXmlCharacters(testCase.StandardError);
                            writer.WriteString(standardError);
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();

                    }

                    writer.WriteEndElement();

                    writer.WriteEndDocument();
                    writer.Close();
                }
            }

            private string RemoveInvalidXmlCharacters(string input)
            {
                return Regex.Replace(input, @"[\u0000-\u0008,\u000B,\u000C,\u000E-\u001F]", "");
            }
        }
    }
}
