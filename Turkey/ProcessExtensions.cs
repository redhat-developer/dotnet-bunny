using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Turkey
{
    public static class ProcessExtensions
    {
        public static async Task WaitForExitAsync(this Process process, CancellationToken token, TextWriter standardOutput, TextWriter standardError)
        {
            process.EnableRaisingEvents = true;
            var outputHandler = new DataReceivedEventHandler(
                (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        standardOutput.WriteLine(e.Data);
                    }
                });
            var errorHandler = new DataReceivedEventHandler(
                (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        standardError.WriteLine(e.Data);
                    }
                });
            process.OutputDataReceived += outputHandler;
            process.BeginOutputReadLine();
            process.ErrorDataReceived += errorHandler;
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(token);
            }
            catch (OperationCanceledException ex)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                { }

                throw;
            }
        }
    }
}
