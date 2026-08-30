using System.Diagnostics;

namespace CodeCrafters.Shell;

public static class ProcessHelper
{
    public static async Task<ProcessDescriptor> RunProcess(FileInfo c,
        string[] args,
        TextWriter stdOut,
        TextWriter stdErr,
        bool redirectStdIn,
        CancellationToken cancellationToken)
    {
        var process = Process.Start(new ProcessStartInfo(c.Name, args)
        {
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory,
            RedirectStandardInput = redirectStdIn,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        });

        if (process is null)
        {
            return new ProcessDescriptor(() => null, Task.CompletedTask, () => true, () => null);
        }

        var processTask = Task.Run(async () =>
        {
            var ctr = cancellationToken.Register(() =>
            {
                if (!process.HasExited)
                {
                    try { process.Kill(); }
                    catch
                    {
                        /* Ignore if it just exited */
                    }
                }
            });
            
            try
            {
                await foreach (var l in process.ReadAllLinesAsync(cancellationToken))
                {
                    var w = l.StandardError ? stdErr : stdOut;
                    await w.WriteLineAsync(l.Content);
                }

                await process.WaitForExitAsync(cancellationToken);
            }
            finally
            {
                await ctr.DisposeAsync();
                process.Dispose();
            }
        });

        return new ProcessDescriptor(() => process.Id,
            processTask,
            () =>
            {
                try
                {
                    return process.HasExited;
                }
                catch
                {
                    return true;
                }
            }, () =>
            {
                try
                {
                    return process.StandardInput;
                }
                catch
                {
                    return null;
                }
            });
    }
}