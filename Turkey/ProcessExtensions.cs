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

            var tcs = new TaskCompletionSource<bool>();
            void CompleteTask(bool cancel)
            {
                if (cancel)
                {
                    tcs.TrySetCanceled();

                    if (!process.HasExited)
                    {
                        process.Kill();;
                    }
                }
                else
                {
                    tcs.TrySetResult(true);
                }

                process.OutputDataReceived -= outputHandler;
                process.ErrorDataReceived -= errorHandler;
            }

            token.Register(() => CompleteTask(cancel: true));
            process.Exited += delegate { CompleteTask(cancel: false); };
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += outputHandler;
            process.BeginOutputReadLine();
            process.ErrorDataReceived += errorHandler;
            process.BeginErrorReadLine();

            if (process.HasExited)
            {
                CompleteTask(cancel: false);
            }

            return tcs.Task;
        }
    }
}
