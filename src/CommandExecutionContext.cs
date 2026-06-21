using System.Diagnostics;

public record CommandExecutionContext(string[] Args, IReadOnlyCollection<Command> AllCommands)
{
    public bool IsHaltRequested { get; private set; }

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
                using var process = Process.Start(c.FullName, ctx.Args);

                /*await foreach (var l in process.ReadAllLinesAsync(cancellationToken: ctx.CancellationToken))
                {
                    Console.WriteLine(l.Content);
                }*/

                await process.WaitForExitAsync(ctx.CancellationToken);
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
