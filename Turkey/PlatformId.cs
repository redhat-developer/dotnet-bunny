using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Turkey
{
    public class PlatformId
    {
        public List<string> CurrentIds
        {
            // TODO make this async?
            get => ComputePlatformIds(File.ReadAllLines("/etc/os-release"), GetLddVersion());
        }

        public List<string> ComputePlatformIds(string[] osReleaseLines, string lddVersionOutput)
        {
            #pragma warning disable CA1308 // Normalize strings to uppercase
            string arch = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
            #pragma warning restore CA1308 // Normalize strings to uppercase
            return ComputePlatformIds(osReleaseLines, arch, lddVersionOutput);
        }

        public List<string> ComputePlatformIds(string[] osReleaseLines, string architecture, string lddVersionOutput)
        {
            var id = GetValue("ID", osReleaseLines);
            id = Unquote(id);
            var versionId = GetValue("VERSION_ID", osReleaseLines);
            versionId = Unquote(versionId);
            var needsLastVersionRemoved = new string[] { "almalinux", "alpine", "ol", "rhel", "rocky" }
                .Any(os => id.Equals(os, StringComparison.Ordinal));
            if (needsLastVersionRemoved)
            {
                int indexOfDot = versionId.LastIndexOf(".", StringComparison.Ordinal);
                if (indexOfDot > 0)
                {
                    versionId = versionId.Substring(0, indexOfDot);
                }
            }
            var platforms = new List<string>()
            {
                "linux",
                "linux" + "-" + architecture,
                id,
                id + "-" + architecture,
                id + versionId,
                id + "." + versionId,
                id + "." + versionId + "-" + architecture
            };

            bool isMusl = lddVersionOutput.Contains("musl", StringComparison.Ordinal);

            if (isMusl)
            {
                platforms.AddRange(new []
                {
                    "linux-musl",
                    "linux-musl" + "-" + architecture,
                });

            }
            return platforms.ToList();
        }

        private static string GetValue(string key, string[] lines)
        {
            return lines.Where(line => line.StartsWith(key + "=", StringComparison.Ordinal)).Last().Substring((key + "=").Length);
        }

        private static string Unquote(string text)
        {
            // TODO implement proper un-escaping
            // This is a limited shell-style syntax described at
            // https://www.freedesktop.org/software/systemd/man/os-release.html
            if (text.StartsWith("\"", StringComparison.Ordinal) && text.EndsWith("\"", StringComparison.Ordinal))
            {
                return text.Substring(1, text.Length - 2);
            }

            return text;
        }

#pragma warning disable CA1822 // Mark members as static
        internal string GetLddVersion()
#pragma warning restore CA1822 // Mark members as static
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = "ldd";
                p.StartInfo.Arguments = "--version";
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                p.WaitForExit();

                return string.Concat(p.StandardOutput.ReadToEnd(), p.StandardError.ReadToEnd());
            }
        }
    }
}
