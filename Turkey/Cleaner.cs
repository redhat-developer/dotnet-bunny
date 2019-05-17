using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Turkey
{
    public class Cleaner
    {
        /// These path must all be directories
        public static IEnumerable<string> CruftDirectoryGlobs()
        {
            yield return "~/.aspnet";
            yield return "~/.dotnet";
            yield return "~/.local/share/NuGet";
            yield return "~/.nuget/packages";
            yield return "~/.templatengine";

            yield return "/tmp/clr-debug-pipe*";
            yield return "/tmp/CoreFxPipe*";
            yield return "/tmp/.dotnet";
            yield return "/tmp/.NETCore*";
            yield return "/tmp/.NETFramework*";
            yield return "/tmp/.NETStandard*";
            yield return "/tmp/NuGet";
            yield return "/tmp/NuGetScratch";
            yield return "/tmp/Razor-Server";
            yield return "/tmp/VBCSCompiler";
        }

        public async Task CleanLocalDotNetCache()
        {
            foreach (var path in CruftDirectoryGlobs())
            {
                foreach(var expanded in ExpandPath(path))
                {
                    // Console.WriteLine("Deleting: " + expanded);
                    Directory.Delete(expanded, true);
                }
            }
            return;
        }

        public IEnumerable<string> ExpandPath(string pathWithGlob)
        {
            if (pathWithGlob.StartsWith("~"))
            {
                pathWithGlob = Environment.GetEnvironmentVariable("HOME") + pathWithGlob.Substring(1);
            }
            var parentDir = Path.GetDirectoryName(pathWithGlob);
            var remainder = Path.GetFileName(pathWithGlob);
            var result = Directory.GetDirectories(parentDir, remainder);
            return result;
        }
    }
}
