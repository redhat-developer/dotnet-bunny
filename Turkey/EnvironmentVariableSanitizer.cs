using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;


namespace Turkey
{
    public class EnvironmentVariableSanitizer
    {  
       private readonly List<string> ToFilter = new List<string>()
        {
            "OPENSSL_CONF",
            "ASPNETCORE_URLS",
            "NUGET_PACKAGES",
        };

        private readonly List<string> FilterPrefixes = new List<string>()
        {
            "DOTNET_",
            "CLR_",
        };

        private readonly List<string> AllowList = new List<string>()
        {
            "DOTNET_ROOT",
        };

        public Dictionary<string, string> SanitizeCurrentEnvironmentVariables()
        {
            return SanitizeEnvironmentVariables(Environment.GetEnvironmentVariables());
        }

        public Dictionary<string, string> SanitizeEnvironmentVariables(IDictionary environmentVariables)
        {
            var result = new Dictionary<string, string>();

            foreach (DictionaryEntry entry in environmentVariables)
            {
                var name = (string)entry.Key;
 
                if (ShouldFilter(name))
                {
                    continue;
                }
 
                result.Add(name, (string)entry.Value);
            }

            return result;
        }

        private bool ShouldFilter(string name)
        {
            if (AllowList.Contains(name))
            {
                return false;
            }
 
            if (ToFilter.Contains(name))
            {
                return true;
            }
 
            return FilterPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
        }
    }
}
