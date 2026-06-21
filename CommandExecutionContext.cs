using System.Diagnostics;

namespace CodeCrafters.Shell;

public enum RedirectionType
{
    Override,
    Append,
}

public record CommandExecutionContext(string[] Args, IReadOnlyCollection<Command> AllCommands)
{
    public bool IsHaltRequested { get; private set; }

    public required TextWriter StdOut { get; init; }

    public required TextWriter StdErr { get; init; }

    public required bool InBackground { get; init; }

    public Command? GetCommand(string command)
    {
        if (GetKnownCommand(command) is { } kc)
        {
            return kc;
        }

        foreach (var d in FileSystemHelper.GetPathDirectories())
        {
            var c = d.EnumerateFiles($"{command}").FirstOrDefault();

            if (c is null)
            {
                continue;
            }

            if (!(c.UnixFileMode.HasFlag(UnixFileMode.GroupExecute)
                  || c.UnixFileMode.HasFlag(UnixFileMode.OtherExecute)
                  || c.UnixFileMode.HasFlag(UnixFileMode.UserExecute)))
            {
                continue;
            }

            return new PathFileCommand(c, async ctx =>
            {
               var p = await ProcessHelper.RunProcess(ctx.InBackground,c, ctx.Args, ctx.StdOut, ctx.StdErr, ctx.CancellationToken);

               if (ctx.InBackground)
               await ctx.StdOut.WriteLineAsync($"[1] {p.Pid}");
            });
        }

        return null;
    }

    public required CancellationToken CancellationToken { get; init; }


    public Command? GetKnownCommand(string command)
    {
        return this.AllCommands.FirstOrDefault(x => command.Equals(x.CommandName, StringComparison.OrdinalIgnoreCase));
    }

    public void Halt()
    {
        this.IsHaltRequested = true;
    }
}

public record ProcessDescriptor(int? Pid);

public static class ProcessHelper
{
   public static async Task<ProcessDescriptor> RunProcess(bool inBg,
    FileInfo c,
    string[] args,
    TextWriter stdOut,
    TextWriter stdErr,
    CancellationToken cancellationToken)
{
    // Removed 'using var' to prevent premature disposal for background tasks.
    var process = Process.Start(new ProcessStartInfo(c.Name, args)
    {
        UseShellExecute = false,
        WorkingDirectory = c.DirectoryName!,
        RedirectStandardError = true,
        RedirectStandardOutput = true,
    });

    if (process is null)
    {
        return new ProcessDescriptor(null);
    }

    // Register cancellation to actually kill the OS process, not just stop awaiting it
    var ctr = cancellationToken.Register(() => 
    {
        if (!process.HasExited) 
        {
            try { process.Kill(); } catch { /* Ignore if it just exited */ }
        }
    });

    if (!inBg)
    {
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
            process.Dispose(); // Safe to dispose here since it ran synchronously
        }

        return new ProcessDescriptor(null);
    }

    // Background execution
    _ = Task.Run(async () =>
    {
        try
        {
            await foreach (var l in process.ReadAllLinesAsync(cancellationToken))
            {
                var w = l.StandardError ? stdErr : stdOut;
                await w.WriteLineAsync(l.Content);
            }
            
            // Wait for it to actually finish before disposing
            await process.WaitForExitAsync(cancellationToken); 
        }
        catch (OperationCanceledException)
        {
            // Expected if canceled, process was killed by the registered callback
        }
        finally
        {
            await ctr.DisposeAsync();
            process.Dispose(); // Transfer ownership of disposal to the background task
        }
    }, cancellationToken);

    return new ProcessDescriptor(process.Id);
}
}

public static class FileSystemHelper
{
    public static IEnumerable<DirectoryInfo> GetPathDirectories()
    {
        var env = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        var dirs = env.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var dir in dirs)
        {
            var d = new DirectoryInfo(dir);

            if (!d.Exists)
            {
                continue;
            }

            yield return d;
        }
    }
}
