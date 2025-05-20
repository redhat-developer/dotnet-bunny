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

        public static System.Uri GetBranchContentUrl(Version version)
        {
            var branchName = "release/" + version.MajorMinor + ".1xx";
            var url = $"https://raw.githubusercontent.com/dotnet/installer/{branchName}/";
            Uri uri = new(url);
            return uri;
        }

        public async Task<string> GetProdConFeedAsync(Version version)
        {
            if (version.Major > 4)
            {
                throw new ArgumentException("No prodcon for .NET 5 or later");
            }

            var url = GetBranchContentUrl(version) + "ProdConFeed.txt";
            Uri uri = new(url);
            var feedUrl = await _client.GetStringAsync(uri).ConfigureAwait(false);
            Uri feedUri = new(feedUrl);
            using(var response = await _client.GetAsync(feedUri).ConfigureAwait(false))
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
            Uri uri = new(url);

            string nugetConfig = null;
            try
            {
                nugetConfig = await _client.GetStringAsync(uri).ConfigureAwait(false);
            }
            catch( HttpRequestException e )
            {
                Console.WriteLine($"WARNING: {e.ToString()}");
            }

            return nugetConfig;
        }
    }
}
