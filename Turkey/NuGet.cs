using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Turkey
{
    public class NuGet
    {
        private readonly HttpClient _client;

        public NuGet(HttpClient client)
        {
            _client = client;
        }

        public async Task<bool> IsPackageLiveAsync(string name, Version version)
        {
            var url = $"https://api-v2v3search-0.nuget.org/autocomplete?id={name}&prerelease=true";
            var result = await _client.GetStringAsync(url);
            return await IsPackageLiveAsync(name, version, result);
        }

        public async Task<bool> IsPackageLiveAsync(string name, Version version, string json)
        {
            JObject deserialized = (JObject) JsonConvert.DeserializeObject(json);
            JArray versions = (JArray) deserialized.GetValue("data");
            var found = versions.Children<JToken>()
                .Where(v => v.Value<string>().Equals(version.ToString()))
                .Any();
            return found;
        }

        public async Task<string> GenerateNuGetConfig(List<string> urls, string nugetConfig = null)
        {
            if( !urls.Any() && nugetConfig == null )
                throw new ArgumentNullException();

            string sources = null;
            if( urls.Any() )
            {
                var sourceParts = new List<string>(urls.Count);
                for( int i = 0; i < urls.Count; i++ )
                {
                    sourceParts.Add($"<add key=\"{i}\" value=\"{urls[i]}\" />");
                }

                sources = string.Join("\n    ", sourceParts);
                if( !string.IsNullOrEmpty(sources) )
                {
                    sources = $"    {sources}\n";
                }
            }

            if( string.IsNullOrWhiteSpace(nugetConfig) )
            {
                nugetConfig = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                              "<configuration>\n" +
                              "  <packageSources>\n" +
                              "  </packageSources>\n" +
                              "</configuration>";
            }

            if( !string.IsNullOrWhiteSpace(sources) )
                nugetConfig = nugetConfig.Replace("</packageSources>", sources + "</packageSources>");

            return nugetConfig;
        }

    }
}
