using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Turkey
{
    public static class ProcessExtensions
    {
        public static Task WaitForExitAsync(this Process process, CancellationToken token, TextWriter standardOutput, TextWriter standardError)
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

            var tcs = new TaskCompletionSource<bool>();
            CancellationTokenRegistration ctr = default;
            ctr = token.Register(() => CompleteTask(cancel: true));
            process.Exited += delegate { CompleteTask(cancel: false); };
            if (process.HasExited) // In case the Process exited before we've added the handler.
            {
                CompleteTask(cancel: false);
            }
            return tcs.Task;

            void CompleteTask(bool cancel)
            {
                if (cancel)
                {
                    bool completed = tcs.TrySetCanceled();

                    if (completed && !process.HasExited)
                    {
                        process.Kill(); ;
                    }
                }
                else
                {
                    process.WaitForExit();  // Wait until we've received all output.
                    ctr.Dispose();          // Don't root objects via the CancellationToken.

                    tcs.TrySetResult(true);
                }

                process.OutputDataReceived -= outputHandler;
                process.ErrorDataReceived -= errorHandler;
            }
        }
    }
}
