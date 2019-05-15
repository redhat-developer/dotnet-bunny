using System.Collections.Generic;
using System.Threading.Tasks;

namespace Turkey
{
    public class Cleaner
    {
        public async Task CleanLocalCache()
        {
            foreach (string dir in new string[] { "~/.nuget/packages", "~/.local/share/NuGet/"})
            {
                // TODO
            }
            return;
        }
    }
}
