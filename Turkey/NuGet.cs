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
            JObject deserialized = (JObject) JsonConvert.DeserializeObject(result);
            JArray versions = (JArray) deserialized.GetValue("data");
            var found = versions.Children<JToken>()
                .Where(v => v.Value<string>() == version.ToString())
                .Any();
            return found;
        }

        public async Task<string> GenerateNuGetConfig(List<string> urls)
        {
            // TODO
            return "";
        }

    }
}
