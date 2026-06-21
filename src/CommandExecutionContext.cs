using System.Diagnostics;

public enum RedirectionType
{
    Override,
    Append
}
public record CommandExecutionContext(string[] Args, IReadOnlyCollection<Command> AllCommands)
{
    public bool IsHaltRequested { get; private set; }
    public required Dictionary<int, (RedirectionType Type, string Target)> Redirections { get; init; }

    public TextWriter StdOut
    {
        get
        {
            if (!Redirections.TryGetValue(1, out var t))
            {
                return Console.Out;
            }

            return new StreamWriter(File.Open(t.Target, t.Type switch
            {
                RedirectionType.Append => FileMode.Append,
                _ => FileMode.Create,
            }));
        }
    }
    
    public TextWriter StdErr
    {
        get
        {
            if (!Redirections.TryGetValue(2, out var t))
            {
                return Console.Error;
            }

            return new StreamWriter(File.Open(t.Target, t.Type switch
            {
                RedirectionType.Append => FileMode.Append,
                _                      => FileMode.Create,
            }));
        }
    }

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
                var pwd = Environment.CurrentDirectory;

                Environment.CurrentDirectory = c.DirectoryName!;

                try
                {
                    using var process = Process.Start(new ProcessStartInfo(c.Name, ctx.Args)
                    {
                        UseShellExecute = true,
                    });

                    if (process is null)
                    {
                        return;
                    }

                    await foreach (var l in process.ReadAllLinesAsync(cancellationToken: ctx.CancellationToken))
                    {
                        var w = l.StandardError ? ctx.StdErr : ctx.StdOut;
                        
                        await w.WriteLineAsync(l.Content);
                    }

                    await process.WaitForExitAsync(ctx.CancellationToken);
                }
                finally
                {
                    Environment.CurrentDirectory = pwd;
                }
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
