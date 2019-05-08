using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Turkey
{
    public class PlatformId
    {
        public List<string> CurrentIds
        {
            get
            {
                // TODO make this async?
                return GetPlatformIdsFromOsRelease(File.ReadAllLines("/etc/os-release"));
            }
        }

        public List<string> GetPlatformIdsFromOsRelease(string[] lines)
        {
            var id = GetValue("ID", lines);
            var versionId = GetValue("VERSION_ID", lines);
            if (id.Equals("rhel", StringComparison.Ordinal))
            {
                int indexOfDot = versionId.IndexOf(".");
                if (indexOfDot > 0)
                {
                    versionId = versionId.Substring(0, versionId.IndexOf("."));
                }
            }
            var platforms = new string[] { "linux", id, id + versionId };
            return platforms.ToList();
        }

        private string GetValue(string key, string[] lines)
        {
            return lines.Where(line => line.StartsWith(key + "=", StringComparison.Ordinal)).Last().Substring((key + "=").Length);
        }
    }
}
