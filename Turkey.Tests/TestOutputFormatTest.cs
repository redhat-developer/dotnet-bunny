using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Turkey.Tests
{
    public class JUnitOutputTest
    {
        [Fact]
        public async Task EmptyResultsProduceBasicXml()
        {
            var resultsFile = new FileInfo(Path.GetTempFileName());

            var j = new TestOutputFormats.JUnitOutput(resultsFile);
            await j.AfterRunningAllTestsAsync(null);
            var xml = File.ReadAllText(resultsFile.FullName);

            var expectedXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<testsuite name=""dotnet"" tests=""0"" failures=""0"" errors=""0"" />";

            Assert.Equal(expectedXml, xml);

            resultsFile.Delete();
        }

        [Fact]
        public async Task SingleTestWithPassingResultProducesValidXml()
        {
            var resultsFile = new FileInfo(Path.GetTempFileName());

            var j = new TestOutputFormats.JUnitOutput(resultsFile);
            var result = new TestResult(TestStatus.Passed, "", "");
            await j.AfterRunningTestAsync("foo", result, new TimeSpan(0));
            await j.AfterRunningAllTestsAsync(null);
            var xml = File.ReadAllText(resultsFile.FullName);

            var expectedXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<testsuite name=""dotnet"" tests=""1"" failures=""0"" errors=""0"">
  <testcase name=""foo"" classname=""TestSuite"">
    <system-out></system-out>
    <system-err></system-err>
  </testcase>
</testsuite>";

            Assert.Equal(expectedXml, xml);

            resultsFile.Delete();
        }

        [Fact]
        public async Task ControlCharactersInTestOutputAreNotPresentInXml()
        {
            var resultsFile = new FileInfo(Path.GetTempFileName());

            var j = new TestOutputFormats.JUnitOutput(resultsFile);
            var result = new TestResult(TestStatus.Passed,
                                        standardOutput: "\u0001\u0002\u0003\u0004\u0005aaa\u0006\u0007\u0008",
                                        standardError: "\u001A\u001B\u001Cbbb\u001d");
            await j.AfterRunningTestAsync("foo", result, new TimeSpan(0));
            await j.AfterRunningAllTestsAsync(null);
            var xml = File.ReadAllText(resultsFile.FullName);

            var expectedXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<testsuite name=""dotnet"" tests=""1"" failures=""0"" errors=""0"">
  <testcase name=""foo"" classname=""TestSuite"">
    <system-out>aaa</system-out>
    <system-err>bbb</system-err>
  </testcase>
</testsuite>";

            Assert.Equal(expectedXml, xml);

            resultsFile.Delete();
        }
    }
}
