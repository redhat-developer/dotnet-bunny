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

        public string GenerateNuGetConfig(List<string> urls)
        {
            var sourceParts = new List<string>(urls.Count);
            for (int i = 0; i < urls.Count; i++)
            {
                sourceParts.Add($"<add key=\"{i}\" value=\"{urls[i]}\" />");
            }
            var sources = string.Join(Environment.NewLine + "    ", sourceParts);
            if (!string.IsNullOrEmpty(sources))
            {
                sources = "    " + sources + Environment.NewLine;
            }

            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine +
                "<configuration>" + Environment.NewLine +
                "  <packageSources>" + Environment.NewLine +
                sources +
                "  </packageSources>" + Environment.NewLine +
                "</configuration>";
        }

    }
}
