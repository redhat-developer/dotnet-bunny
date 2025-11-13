using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Turkey
{
    public static class ProcessRunner
    {
        public static async Task<int> RunAsync(ProcessStartInfo psi, Action<string> logger, CancellationToken token)
        {
            logger($"Executing {psi.FileName} with arguments {psi.Arguments} in working directory {psi.WorkingDirectory}");
            using var process = Process.Start(psi);
            await process.WaitForExitAsync(logger, token).ConfigureAwait(false);
            return process.ExitCode;
        }

        public static string Run(string filename, params string[] args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = filename,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }
            using (Process p = Process.Start(startInfo))
            {
                p.StandardInput.Close();
                string stdout = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    string stderr = p.StandardError.ReadToEnd();
                    throw new InvalidOperationException($"Executing {filename} {string.Join(' ', args)} failed with exit code {p.ExitCode} and stderr: {stderr}");
                }
                return stdout;
            }
        }
    }

    public static class ProcessExtensions
    {
        public static async Task WaitForExitAsync(this Process process, Action<string> logger, CancellationToken token)
        {
            process.EnableRaisingEvents = true;
            bool captureOutput = true;
            DataReceivedEventHandler logToLogger = (sender, e) =>
            {
                if (e.Data != null)
                {
                    lock (logger)
                    {
                        if (captureOutput)
                        {
                            logger(e.Data);
                        }
                    }
                }
            };
            process.OutputDataReceived += logToLogger;
            process.BeginOutputReadLine();
            process.ErrorDataReceived += logToLogger;
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(token).ConfigureAwait(false);

                logger($"Process Exit Code: {process.ExitCode}");
            }
            catch (OperationCanceledException)
            {
                lock (logger)
                {
                    captureOutput = false;
                }
                logger($"Process wait for exit cancelled.");

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
