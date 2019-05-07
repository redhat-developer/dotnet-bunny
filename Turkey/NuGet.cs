using System.Collections.Generic;
using System.Threading.Tasks;

namespace Turkey
{
    public class NuGet
    {
        public static async Task<bool> IsPackageLiveAsync(string name, string version)
        {
            return false;
        }

        public static async Task<string> GetProdConFeedAsync(string branchName)
        {
            return "";
        }

        public static async Task<string> GenerateNuGetConfig(List<string> urls)
        {
            return "";
        }

        public static async Task CleanLocalCache()
        {
            foreach (string dir in new string[] { "~/.nuget/packages", "~/.local/share/NuGet/"})
            {

            }
            return;
        }
    }
}
