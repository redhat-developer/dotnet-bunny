using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Turkey
{
    public class LogWriter
    {
        private readonly DirectoryInfo logDirectory;

        public LogWriter(DirectoryInfo logDirectory)
        {
            this.logDirectory = logDirectory;
        }

        public async Task WriteAsync(string testName, string standardOutput, string standardError)
        {
            var logFileName = $"logfile-{testName}.log";
            var path = Path.Combine(logDirectory.FullName, logFileName);
            using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                await WriteAsync(sw, standardOutput, standardError);
            }
        }

        protected async Task WriteAsync(StreamWriter writer, string standardOutput, string standardError)
        {
            await writer.WriteAsync("# Standard Output:" + Environment.NewLine);
            await writer.WriteAsync(standardOutput);
            await writer.WriteAsync("# Standard Error:" + Environment.NewLine);
            await writer.WriteAsync(standardError);
        }
    }

}
