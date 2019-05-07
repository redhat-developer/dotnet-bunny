using System;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Turkey
{
    public abstract class TestParser
    {
        public static async Task<Test> ParseAsync(FileInfo testConfiguration)
        {
            // TODO: async
            JsonSerializer serializer = new JsonSerializer();

            using (JsonReader reader = new JsonTextReader(testConfiguration.OpenText()))
            {
                JObject obj = (JObject) serializer.Deserialize(reader);
                var test = new BashTest(testConfiguration.Directory);
                test.Name = obj.GetValue("name").ToString();
                return test;
            }

        }
    }
}
