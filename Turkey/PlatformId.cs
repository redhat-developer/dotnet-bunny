using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Turkey
{
    public class PlatformId
    {
        public List<string> CurrentIds
        {
            // TODO make this async?
            get => GetPlatformIdsFromOsRelease(File.ReadAllLines("/etc/os-release"));
        }

        public List<string> GetPlatformIdsFromOsRelease(string[] lines)
        {
            string arch = Enum.GetName(typeof(Architecture), RuntimeInformation.OSArchitecture).ToLowerInvariant();
            return GetPlatformIdsFromOsRelease(lines, arch);
        }

        public List<string> GetPlatformIdsFromOsRelease(string[] lines, string architecture)
        {
            var id = GetValue("ID", lines);
            id = Unquote(id);
            var versionId = GetValue("VERSION_ID", lines);
            versionId = Unquote(versionId);
            if (id.Equals("rhel", StringComparison.Ordinal))
            {
                int indexOfDot = versionId.IndexOf(".", StringComparison.Ordinal);
                if (indexOfDot > 0)
                {
                    versionId = versionId.Substring(0, indexOfDot);
                }
            }
            var platforms = new string[] {
                "linux",
                "linux" + "-" + architecture,
                id,
                id + "-" + architecture,
                id + versionId,
                id + "." + versionId,
                id + "." + versionId + "-" + architecture };
            return platforms.ToList();
        }

        private string GetValue(string key, string[] lines)
        {
            return lines.Where(line => line.StartsWith(key + "=", StringComparison.Ordinal)).Last().Substring((key + "=").Length);
        }

        private string Unquote(string text)
        {
            // TODO implement proper un-escaping
            // This is a limited shell-style syntax described at
            // https://www.freedesktop.org/software/systemd/man/os-release.html
            if (text.StartsWith("\"") && text.EndsWith("\""))
            {
                return text.Substring(1, text.Length - 2);
            }

            return text;
        }
    }
}
