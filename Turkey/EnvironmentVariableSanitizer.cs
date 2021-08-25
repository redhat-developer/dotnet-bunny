using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Turkey
{
    public class EnvironmentVariableSanitizer
    {
        private readonly List<string> ToFilter = new List<string>()
        {
            "OPENSSL_CONF",
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
                if (ToFilter.Contains((string)entry.Key))
                {
                    continue;
                }

                result.Add((string)entry.Key, (string)entry.Value);
            }

            return result;
        }
    }
}
