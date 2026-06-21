using System.Collections.Concurrent;
using System.Diagnostics;

namespace CodeCrafters.Shell;

public enum RedirectionType
{
    Override,
    Append,
}

public static class BackgroundJobStorage
{
    public static ConcurrentDictionary<int, (CliRawCommand Command, ProcessDescriptor Process)> Jobs { get; } = [];
    
    public static async Task WriteAndReap(TextWriter writer, bool skipRunning)
    {
        var jobs = BackgroundJobStorage.Jobs.ToDictionary();

        foreach (var (i, (number, (command, p))) in jobs.OrderBy(x => x.Key).Index())
        {
            var exited = p.Exited();

            if (!exited && skipRunning)
            {
                continue;
            }

            var status = exited ? "Done" : "Running";

            status = $" {status}".PadRight(24, ' ');

            var lastness = i == jobs.Count - 1
                               ? "+"
                               : i == jobs.Count - 2
                                   ? "-" : " ";

            await writer.WriteLineAsync($"[{number}]{lastness} {status}{command.OriginalInput.TrimEnd('&').Trim()}");

            if (exited)
            {
                BackgroundJobStorage.Jobs.TryRemove(number, out _);
            }
        }
    }
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
                var p = await ProcessHelper.RunProcess(ctx.InBackground, c, ctx.Args, ctx.StdOut, ctx.StdErr, ctx.CancellationToken);
 
                if (ctx.InBackground)
                {
                    var j = 1;

                    var jobs = BackgroundJobStorage.Jobs.Select(x => x.Key).Order().ToList();

                    for (var i = 0; i < jobs.Count; i++)
                    {
                        if (i+1 != jobs[i])
                        {
                            j = i + 1;
                            break;
                        }
                    }

                    var start = DateTime.Now;

                    await ctx.StdOut.WriteLineAsync($"[{j}] {p.Pid()}");

                    /*_ = Task.Run(async () =>
                    {
                        await p.ExitTask;

                        BackgroundJobStorage.Jobs.TryRemove(j, out var _);
                    }, this.CancellationToken);*/

                    BackgroundJobStorage.Jobs.TryAdd(j, (ctx.RawCommand, p));
                }
            });
        }

        return null;
    }

    public required CliRawCommand RawCommand { get; set; }

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

public record ProcessDescriptor(Func<int?> Pid, Task ExitTask, Func<bool> Exited);

public static class ProcessHelper
{
    public static async Task<ProcessDescriptor> RunProcess(bool inBg,
        FileInfo c,
        string[] args,
        TextWriter stdOut,
        TextWriter stdErr,
        CancellationToken cancellationToken)
    {
        var process = Process.Start(new ProcessStartInfo(c.Name, args)
        {
            UseShellExecute = false,
            WorkingDirectory = c.DirectoryName!,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        });

        if (process is null)
        {
            return new ProcessDescriptor(() => null, Task.CompletedTask, () => true);
        }

        if (!inBg)
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

            return new ProcessDescriptor(() => null, Task.CompletedTask, () => true);
        }

        _ = Task.Run(async () =>
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
                process.Dispose();
            }
        }, cancellationToken);

        return new ProcessDescriptor(() => process.Id, process.WaitForExitAsync(cancellationToken), () =>
        {
            try
            {
                return process.HasExited;
            }
            catch
            {
                return true;
            }
        });
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
