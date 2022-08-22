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

        public string GetBranchContentUrl(Version version)
        {
            string url;
            if (version.Major <= 3)
            {
                var branchName = "release/" + version.MajorMinor;
                url = $"https://raw.githubusercontent.com/dotnet/source-build/{branchName}/";
            }
            else
            {
                var branchName = "release/" + version.MajorMinor + ".1xx";
                url = $"https://raw.githubusercontent.com/dotnet/installer/{branchName}/";
            }

            return url;
        }

        public async Task<string> GetProdConFeedAsync(Version version)
        {
            if (version.Major > 4)
            {
                throw new ArgumentException("No prodcon for .NET 5 or later");
            }

            var url = GetBranchContentUrl(version) + "ProdConFeed.txt";
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
            string url = GetBranchContentUrl(version) + "NuGet.config";

            string nugetConfig = null;
            try
            {
                nugetConfig = await _client.GetStringAsync(url);
            }
            catch( HttpRequestException e )
            {
                Console.WriteLine($"WARNING: {e.ToString()}");
            }

            return nugetConfig;
        }
    }
}
