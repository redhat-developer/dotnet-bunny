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

        public Task<string> GetProdConFeedAsync(Version version)
        {
            var branchName = "branch/" + version.MajorMinor;
            return GetProdConFeedAsync(branchName);
        }

        public async Task<string> GetProdConFeedAsync(string branchName)
        {
            try
            {
                var url = $"https://raw.githubusercontent.com/dotnet/source-build/{branchName}/ProdConFeed.txt";
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
            catch (HttpRequestException e)
            {
                return null;
            }
        }
    }
}
