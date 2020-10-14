using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Turkey
{
    public class CsprojCompatibilityPatcher
    {
        public string Patch(string originalCsprojContents, Version newRuntime)
        {
            var pattern = @"<TargetFramework>net(?:coreapp)?\d\.\d+</TargetFramework>";
            var versionString = newRuntime.MajorMinor;
            string replacement = null;
            if (newRuntime.Major < 4)
            {
                replacement = $"<TargetFramework>netcoreapp{versionString}</TargetFramework>";
            }
            else
            {
                replacement = $"<TargetFramework>net{versionString}</TargetFramework>";
            }
            var output = Regex.Replace(originalCsprojContents, pattern, replacement);

            return output;
        }

    }

}
