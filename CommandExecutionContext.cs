namespace CodeCrafters.Shell;

public record CommandExecutionContext(string[] Args, IReadOnlyCollection<Command> AllCommands)
{
    private TextWriter? StdIn { get;  set; }
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
            FileInfo? c;

            try
            {
                c = d.EnumerateFiles($"{command}").FirstOrDefault();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Cannot enumerate search pattern: '{command}' in {d.FullName}: {e.Message}");

                throw;
            }

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
                var p = await ProcessHelper.RunProcess(c,
                            ctx.Args,
                            ctx.StdOut,
                            ctx.StdErr,
                            ctx.RedirectStdIn,
                            ctx.CancellationToken);

                ctx.SetStdIn(p.StdIn());
 
                if (ctx.InBackground)
                {
                    var j = -1;

                    var jobs = BackgroundJobStorage.Jobs.Select(x => x.Key).Order().ToList();

                    for (var i = 0; i < jobs.Count; i++)
                    {
                        if (i+1 != jobs[i])
                        {
                            j = i + 1;
                            break;
                        }
                    }

                    if (j == -1)
                    {
                        j = jobs.Count + 1;
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
                else
                {
                    await p.ExitTask;
                }
            });
        }

        return null;
    }

    private void SetStdIn(TextWriter? textWriter)
    {
        StdIn = textWriter;

        foreach (var action in this.stdInCaptureCallbacks)
        {
            action(textWriter);
        }
    }

    public bool RedirectStdIn { get; set; }

    public IDisposable OnStdInCaptured(Action<TextWriter?> callback)
    {
        return new Unsubscriber(this, callback);
    }

    private List<Action<TextWriter?>> stdInCaptureCallbacks = [];

    private class Unsubscriber : IDisposable
    {
        private readonly CommandExecutionContext parent;
        private readonly Action<TextWriter?> callback;

        public Unsubscriber(CommandExecutionContext parent, Action<TextWriter?> callback)
        {
            this.parent = parent;
            this.callback = callback;
            
            this.parent.stdInCaptureCallbacks.Add(callback);
        }
        
        public void Dispose()
        {
            this.parent.stdInCaptureCallbacks.Remove(this.callback);
        }
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