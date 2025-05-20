using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Turkey
{
    public static class TestOutputFormats
    {
        internal class NewOutput : TestOutput
        {
            public class FailedTest
            {
                public string Name{ set; get;}
                public string Duration{ set; get;}
            }

            private List<FailedTest> failedTests = new List<FailedTest>();

#pragma warning disable CA1822 // Mark members as static
            public void AtStartup()
#pragma warning restore CA1822 // Mark members as static
            {
                Console.WriteLine("Running tests:");
                return;
            }
            public override Task AfterParsingTestAsync(string name, bool enabled)
            {
                #pragma warning disable CA1305
                var nameText = string.Format("{0,-60}", name);
                #pragma warning restore CA1305
                Console.Write(nameText);
                return Task.CompletedTask;
            }

            public override Task AfterRunningTestAsync(string name, TestResult result, StringBuilder testLog, TimeSpan testTime)
            {
                int minutes = (int)testTime.TotalMinutes;
                int seconds = (int)Math.Ceiling(testTime.TotalSeconds - 60 * minutes);
                string elapsedTime = minutes == 0 ? $"{seconds}s"
                                                  : $"{minutes}m {seconds}s";
                string resultOutput = null;
                if (Console.IsOutputRedirected || Console.IsErrorRedirected)
                {
                    switch (result)
                    {
                        case TestResult.Passed: resultOutput = "PASS"; break;
                        case TestResult.Failed: resultOutput = "FAIL"; failedTests.Add(new FailedTest {Name = name, Duration = elapsedTime}); break;
                        case TestResult.Skipped: resultOutput = "SKIP"; break;
                    }
                    Console.WriteLine($"[{resultOutput}]\t({elapsedTime})");
                }
                else
                {
                    switch (result)
                    {
                        case TestResult.Passed: resultOutput = "\u001b[32mPASS\u001b[0m"; break;
                        case TestResult.Failed: resultOutput = "\u001b[31mFAIL\u001b[0m"; failedTests.Add(new FailedTest {Name = name, Duration = elapsedTime}); break;
                        case TestResult.Skipped: resultOutput = "SKIP"; break;
                    }
                    Console.WriteLine($"[{resultOutput}]\t({elapsedTime})");
                }
                return Task.CompletedTask;
            }

            public override Task PrintFailedTests()
            {
                Console.WriteLine();
                Console.WriteLine("The following tests failed: ");
                foreach(var test in failedTests)
                {
                    #pragma warning disable CA1305
                    Console.WriteLine($"{string.Format("{0,-30}", test.Name)}({test.Duration})");
                    #pragma warning restore CA1305
                }
                return Task.CompletedTask;
            }

            public override Task AfterRunningAllTestsAsync(TestResults results)
            {
                Console.WriteLine();
                Console.WriteLine($"Total: {results.Total} Passed: {results.Passed} Failed: {results.Failed}");
                return Task.CompletedTask;
            }
        }

        internal class JUnitOutput : TestOutput
        {
            private struct TestCase {
                public string Name;
                public string ClassName;
                public bool Failed;
                public bool Skipped;
                public string Message;
                public StringBuilder Log;
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

            public override Task AfterRunningTestAsync(string name, TestResult result, StringBuilder testLog, TimeSpan testTime)
            {
                var testCase = new TestCase();
                testCase.Name = name;
                testCase.ClassName = "TestSuite";
                testCase.Failed = result == TestResult.Failed;
                testCase.Skipped = result == TestResult.Skipped;
                testCase.Message = "see log";
                testCase.Log = testLog;

                _testCases.Add(testCase);

                return Task.CompletedTask;
            }

            public override Task AfterRunningAllTestsAsync(TestResults results)
            {
                var settings = new XmlWriterSettings();
                settings.Indent = true;

                using (var writer = XmlWriter.Create(_resultsFile.FullName, settings))
                {
                    writer.WriteStartDocument();

                    writer.WriteStartElement("testsuite");
                    writer.WriteAttributeString("name", "dotnet");
#pragma warning disable CA1305 // Specify IFormatProvider
                    writer.WriteAttributeString("tests", _testCases.Count.ToString());
                    writer.WriteAttributeString("failures", _testCases.Where(t => t.Failed).Count().ToString());
#pragma warning restore CA1305 // Specify IFormatProvider
                    writer.WriteAttributeString("errors", "0");

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

                        if (testCase.Log != null)
                        {
                            writer.WriteStartElement("system-out");
                            string standardOutput = RemoveInvalidXmlCharacters(testCase.Log.ToString());
                            writer.WriteString(standardOutput);
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();

                    }

                    writer.WriteEndElement();

                    writer.WriteEndDocument();
                    writer.Close();
                }
                return Task.CompletedTask;
            }

            private static string RemoveInvalidXmlCharacters(string input)
            {
                return Regex.Replace(input, @"[\u0000-\u0008,\u000B,\u000C,\u000E-\u001F]", "");
            }
        }
    }
}
