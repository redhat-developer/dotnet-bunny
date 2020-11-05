using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Turkey
{
    /// <summary>
    ///   Work with https://github.com/dotnet/source-build
    /// </summary>
    public class SourceBuild
    {
        private readonly HttpClient _client;

        public SourceBuild(HttpClient client)
        {
            this._client = client;
        }

        public string GetBranch(Version version)
        {
            // FIXME: hack to treat 5.0 as special for now
            if (version.Major == 5)
            {
                return "master";
            }
            return "release/" + version.MajorMinor;
        }

        public async Task<string> GetProdConFeedAsync(Version version)
        {
            var url = $"https://raw.githubusercontent.com/dotnet/source-build/{GetBranch(version)}/ProdConFeed.txt";
            var feedUrl = await _client.GetStringAsync(url);

            using(var response = await _client.GetAsync(feedUrl))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
            }
            return feedUrl;
        }

        public async Task<string> GetNuGetConfigAsync(Version version)
        {
            string url = $"https://raw.githubusercontent.com/dotnet/source-build/{GetBranch(version)}/NuGet.config";

            string nugetConfig = null;
            try
            {
                nugetConfig = await _client.GetStringAsync(url);
            }
            catch( HttpRequestException e )
            {
                Console.WriteLine($"WARNING: {e.Message}");
            }

            return nugetConfig;
        }
    }
}
