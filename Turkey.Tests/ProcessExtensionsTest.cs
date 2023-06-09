using Xunit;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Turkey.Tests
{
    public class ProcessExtensiosnTests
    {
        [Fact]
        public async Task WaitForExitAsync_DoesNotHangForOrphanedGrandChildren()
        {
            const int WaitTimeoutSeconds = 3;
            const int GrandChildAgeSeconds = 3 * WaitTimeoutSeconds;

            string filename = Path.GetTempFileName();
            try
            {
                // This script creates a 'sleep' grandchild that outlives its parent.
                File.WriteAllText(filename,
                    $"""
                    #!/bin/bash

                    sleep {GrandChildAgeSeconds} &
                    """);
                File.SetUnixFileMode(filename, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

                var psi = new ProcessStartInfo()
                {
                    FileName = filename,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using Process process = Process.Start(psi);

                // Use a shorter timeout for WaitForExitAsync than the grandchild lives.
                long startTime = Stopwatch.GetTimestamp();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(WaitTimeoutSeconds));

                // The WaitForExit completes by cancellation.
                await Assert.ThrowsAsync<TaskCanceledException>(() => process.WaitForExitAsync(cts.Token, new StringWriter(), new StringWriter()));

                // The completion takes at least the WaitTime.
                TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime);
                Assert.True(elapsedTime >= TimeSpan.FromSeconds(WaitTimeoutSeconds), "The grandchild is not keeping the script alive");
            }
            finally
            {
                File.Delete(filename);
            }
        }
    }
}
