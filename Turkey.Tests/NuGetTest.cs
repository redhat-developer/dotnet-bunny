using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;

namespace Turkey.Tests
{
    public class NuGetTest
    {
        [Theory(Skip="Uses network")]
        [InlineData("Microsoft.NETCore.App", "1.0.0", true)]
        [InlineData("Microsoft.NETCore.App", "1.1.0", true)]
        [InlineData("Microsoft.NETCore.App", "1.2.0", false)]
        [InlineData("Microsoft.NETCore.App", "1.3.0", false)]
        [InlineData("Microsoft.NETCore.App", "2.0.0", true)]
        [InlineData("Microsoft.NETCore.App", "2.1.0", true)]
        [InlineData("Microsoft.NETCore.App", "2.1.11", true)]
        [InlineData("Microsoft.NETCore.App", "2.2.0", true)]
        [InlineData("Microsoft.NETCore.App", "2.2.1", true)]
        [InlineData("Microsoft.NETCore.App", "2.3.0", false)]
        [InlineData("Microsoft.NETCore.App", "3.9.0", false)]
        public async Task LivePackagesAreIdentifiedCorrectlyFromNetwork(string name, string version, bool live)
        {
            using (var http = new HttpClient())
            {
                NuGet nuget = new NuGet(http);
                var ver = Version.Parse(version);
                Assert.Equal(live, await nuget.IsPackageLiveAsync(name, ver));
            }
        }

        // curl 'https://api-v2v3search-0.nuget.org/autocomplete?id=microsoft.netcore.app&prerelease=true' | sed 's/"/\\"/g'
        private readonly string json = "{\"@context\":{\"@vocab\":\"http://schema.nuget.org/schema#\"},\"totalHits\":1,\"lastReopen\":\"2019-05-23T23:28:54.6740419Z\",\"index\":\"v3-lucene2-v2v3-20171018\",\"data\":[\"1.0.0-rc2-3002702\",\"1.0.0\",\"1.0.1\",\"1.0.3\",\"1.0.4\",\"1.0.5\",\"1.0.7\",\"1.0.8\",\"1.0.9\",\"1.0.10\",\"1.0.11\",\"1.0.12\",\"1.0.13\",\"1.0.14\",\"1.0.15\",\"1.0.16\",\"1.1.0-preview1-001100-00\",\"1.1.0\",\"1.1.1\",\"1.1.2\",\"1.1.4\",\"1.1.5\",\"1.1.6\",\"1.1.7\",\"1.1.8\",\"1.1.9\",\"1.1.10\",\"1.1.11\",\"1.1.12\",\"1.1.13\",\"2.0.0-preview1-002111-00\",\"2.0.0-preview2-25407-01\",\"2.0.0\",\"2.0.3\",\"2.0.4\",\"2.0.5\",\"2.0.6\",\"2.0.7\",\"2.0.9\",\"2.1.0-preview1-26216-03\",\"2.1.0-preview2-26406-04\",\"2.1.0-rc1\",\"2.1.0\",\"2.1.1\",\"2.1.2\",\"2.1.3\",\"2.1.4\",\"2.1.5\",\"2.1.6\",\"2.1.7\",\"2.1.8\",\"2.1.9\",\"2.1.10\",\"2.1.11\",\"2.2.0-preview-26820-02\",\"2.2.0-preview2-26905-02\",\"2.2.0-preview3-27014-02\",\"2.2.0\",\"2.2.1\",\"2.2.2\",\"2.2.3\",\"2.2.4\",\"2.2.5\",\"3.0.0-preview-27122-01\",\"3.0.0-preview-27324-5\",\"3.0.0-preview3-27503-5\",\"3.0.0-preview4-27615-11\",\"3.0.0-preview5-27626-15\"]}";

        [Theory]
        [InlineData("Microsoft.NETCore.App", "1.0.0", true)]
        [InlineData("Microsoft.NETCore.App", "1.1.0", true)]
        [InlineData("Microsoft.NETCore.App", "1.2.0", false)]
        [InlineData("Microsoft.NETCore.App", "1.3.0", false)]
        [InlineData("Microsoft.NETCore.App", "2.0.0", true)]
        [InlineData("Microsoft.NETCore.App", "2.1.0", true)]
        [InlineData("Microsoft.NETCore.App", "2.1.11", true)]
        [InlineData("Microsoft.NETCore.App", "2.2.0", true)]
        [InlineData("Microsoft.NETCore.App", "2.2.1", true)]
        [InlineData("Microsoft.NETCore.App", "2.3.0", false)]
        [InlineData("Microsoft.NETCore.App", "3.9.0", false)]
        public async Task LivePackagesAreIdentifiedCorrectly(string name, string version, bool live)
        {
            using (var http = new HttpClient())
            {
                NuGet nuget = new NuGet(http);
                var ver = Version.Parse(version);
                Assert.Equal(live, await nuget.IsPackageLiveAsync(name, ver, json));
            }
        }

        [Theory]
        // FIXME [InlineData(new string[] { }, "")]
        [InlineData(new string[] { "foo" }, "<add key=\"0\" value=\"foo\" />")]
        [InlineData(new string[] { "foo" , "bar"}, "<add key=\"0\" value=\"foo\" /> <add key=\"1\" value=\"bar\" />")]
        public async Task NuGetConfigIsGeneratedCorrectly(string[] urls, string feedParts)
        {
            using (var http = new HttpClient())
            {
                NuGet nuget = new NuGet(http);

                var expectedConfig = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <configuration> <packageSources> " + feedParts + " </packageSources> </configuration>";
                var cleanedExpectedConfig = Regex.Replace(expectedConfig, @"\s+", " ");

                var actualConfig = await nuget.GenerateNuGetConfig(urls.ToList());
                var cleanedActualConfig = Regex.Replace(actualConfig, @"\s+", " ");

                Assert.Equal(cleanedExpectedConfig, cleanedActualConfig);
            }
        }
    }
}
