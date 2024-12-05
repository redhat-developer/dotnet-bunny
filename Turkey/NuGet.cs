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
            Uri uri = new(url);
            var result = await _client.GetStringAsync(uri).ConfigureAwait(false);
            return await IsPackageLiveAsync(name, version, result).ConfigureAwait(false);
        }

#pragma warning disable CA1801 // Remove unused parameter
#pragma warning disable CA1822 // Mark members as static
        public Task<bool> IsPackageLiveAsync(string name, Version version, string json)
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore CA1801 // Remove unused parameter
        {
            JObject deserialized = (JObject) JsonConvert.DeserializeObject(json);
            JArray versions = (JArray) deserialized.GetValue("data", StringComparison.Ordinal);
            var found = versions.Children<JToken>()
                .Where(v => v.Value<string>().Equals(version.ToString(), StringComparison.Ordinal))
                .Any();
            return Task.FromResult(found);
        }

#pragma warning disable CA1822 // Mark members as static
        public Task<string> GenerateNuGetConfig(List<string> urls, string nugetConfig = null)
#pragma warning restore CA1822 // Mark members as static
        {
            if (!urls.Any())
                ArgumentNullException.ThrowIfNull(nugetConfig);

            string sources = null;
            if (urls.Any())
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
                nugetConfig = nugetConfig.Replace("</packageSources>", sources + "</packageSources>", StringComparison.Ordinal);

            return Task.FromResult(nugetConfig);
        }

    }
}
