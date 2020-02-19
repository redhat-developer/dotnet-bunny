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

        /// These path must all be directories
        public static IEnumerable<string> LocalProjectCruft()
        {
            yield return "bin";
            yield return "out";
            yield return "project.lock.json";
        }

        public async Task CleanProjectLocalDotNetCruftAsync()
        {

            foreach(var name in LocalProjectCruft())
            {
                // Console.WriteLine("Deleting: " + name);
                if (Directory.Exists(name))
                {
                    Directory.Delete(name, true);
                }
                else if (File.Exists(name))
                {
                    File.Delete(name);
                }
            }
        }

        public async Task CleanLocalDotNetCacheAsync()
        {
            foreach (var path in CruftDirectoryGlobs())
            {
                try
                {
                    foreach(var expanded in ExpandPath(path))
                    {
                        // Console.WriteLine("Deleting: " + expanded);
                        try
                        {
                            Directory.Delete(expanded, true);
                        }
                        catch (IOException)
                        {
                            Console.WriteLine($"WARNING: unable to delete {expanded}");
                        }
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine($"WARNING: unable to expand {path}");
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
