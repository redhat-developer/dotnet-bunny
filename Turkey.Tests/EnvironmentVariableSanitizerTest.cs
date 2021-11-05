using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xunit;

namespace Turkey.Tests
{
    public class EnvironmentVariableSanitizerTest
    {
        [Theory]
        // TODO enable these too
        // [InlineData("ASPNETCORE_URLS")]
        // [InlineData("COREHOST_TRACE")]
        // [InlineData("DOTNET_FOO_BAR")]
        // [InlineData("DOTNET_ROLL_FORWARD")]
        // [InlineData("DOTNET_RUNTIME_ID")]
        // [InlineData("DOTNET_STARTUP_HOOKS")]
        // [InlineData("NUGET_PACKAGES")]
        [InlineData("OPENSSL_CONF")]
        public void EnvironmentVariablesAreRemoved(string name)
        {
            var environment = new Dictionary<string, string>()
            {
                { name, "foobar" },
            };

            var sanitizer = new EnvironmentVariableSanitizer();
            var result = sanitizer.SanitizeEnvironmentVariables(environment);

            Assert.DoesNotContain(name, result.Keys);
        }

        [InlineData("DOTNET_ROOT")]
        [InlineData("DOTNET_CLI_TELEMETRY_OPTOUT")]
        [InlineData("PATH")]
        [InlineData("USER")]
        [InlineData("HOME")]
        public void EnvironmentVariablesAreKept(string name)
        {
            var environment = new Dictionary<string, string>()
            {
                { name, "foobar" },
            };

            var sanitizer = new EnvironmentVariableSanitizer();
            var result = sanitizer.SanitizeEnvironmentVariables(environment);

            Assert.Contains(name, result.Keys);

        }
    }
}
