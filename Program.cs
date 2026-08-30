using CodeCrafters.Shell;
using CodeCrafters.Shell.Commands;

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

while (!cts.Token.IsCancellationRequested)
{
    await BackgroundJobStorage.WriteAndReap(Console.Out, true);

    await Console.Out.WriteAsync("$ ");

    var userLine = await Console.In.ReadLineAsync();

    userLine = userLine?.Trim() ?? string.Empty;
    
    CommandHistoryStore.Commands.Add(userLine);

    var allParsedArguments = CliTokenParser.GetTokens(userLine);

    foreach (var (_, allArgs) in CliPipeParser.GetCommandGroups(allParsedArguments.ToArray()))
    {
        var pipe = CliPipeParser.GetCommandPipe(allArgs);

        var haltRequested = false;

        TextWriter? stdOut = null;

        var tasks = new List<Task>();

        foreach (var (i, arguments) in pipe.Index().Reverse())
        {
            CliRawCommand rawCommand;

            try
            {
                rawCommand = CliCommandParser.ParseCommand(arguments);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while parsing {arguments.ToCollString()}: {e.Message}");

                throw;
            }

            var tsc = new TaskCompletionSource<TextWriter?>();

            var localStdOut = stdOut;

            var pipeTask = Task.Run(async () =>
            {
                Action<TextWriter?>? ca = i > 0 ? tsc.SetResult : null;

                var ctx = await RunCommand(rawCommand,
                              localStdOut,
                              ca,
                              cts.Token);

                if (localStdOut is not null)
                {
                    await localStdOut.DisposeAsync();
                }

                if (ctx.IsHaltRequested)
                {
                    haltRequested = true;
                }

                // if not completed capturing stdin, set no-op writer
                if (!tsc.Task.IsCompleted)
                {
                    tsc.TrySetResult(TextWriter.Null);
                }
            });

            tasks.Add(pipeTask);

            // need to wait for available stdIn from other process
            if (i > 0)
            {
                stdOut = await tsc.Task;
            }
        }

        await Task.WhenAll(tasks);

        if (haltRequested)
        {
            return;
        }
    }
}

static List<Command> GetWellKnownCommands() => new ()
{
    new ExitCommand(),
    new EchoCommand(),
    new TypeCommand(),
    new PwdCommand(),
    new CdCommand(),
    new JobsCommand(),
    new HistoryCommand(),
};

static async Task<CommandExecutionContext> RunCommand(CliRawCommand rawCommand,
    TextWriter? nextStdOut,
    Action<TextWriter?>? onStdInCapture,
    CancellationToken cancellationToken)
{
    TextWriter? stdOut = null;
    TextWriter? stdErr = null;
    IDisposable? stdInCaptureSub = null;

    try
    {
        if (nextStdOut is not null)
        {
            stdOut = nextStdOut;
        }
        else if (rawCommand.Redirections.TryGetValue(1, out var re))
        {
            stdOut = new StreamWriter(File.Open(re.Target, re.Type switch
            {
                RedirectionType.Append   => FileMode.Append,
                RedirectionType.Override => FileMode.Create,
                var _                    => throw new ArgumentOutOfRangeException()
            }, FileAccess.Write));
        }

        if (rawCommand.Redirections.TryGetValue(2, out var se))
        {
            stdErr = new StreamWriter(File.Open(se.Target, se.Type switch
            {
                RedirectionType.Append   => FileMode.Append,
                RedirectionType.Override => FileMode.Create,
                var _                    => throw new ArgumentOutOfRangeException()
            }, FileAccess.Write));
        }

        var ctx = new CommandExecutionContext(rawCommand.Args.ToArray(), GetWellKnownCommands())
        {
            RawCommand = rawCommand,
            CancellationToken = cancellationToken,
            StdOut = stdOut ?? Console.Out,
            StdErr = stdErr ?? Console.Error,
            RedirectStdIn = onStdInCapture is not null,
            InBackground = rawCommand.IsBackground
        };

        if (onStdInCapture is not null)
        {
            stdInCaptureSub = ctx.OnStdInCaptured(onStdInCapture);
        }

        var foundCommand = ctx.GetCommand(rawCommand.Command);

        if (foundCommand is null)
        {
            await ctx.StdOut.WriteLineAsync($"{rawCommand.Command}: command not found");

            onStdInCapture?.Invoke(null);

            return ctx;
        }

        await foundCommand.Handle(ctx);

        return ctx;
    }
    catch (Exception e)
    {
        throw;
    }
    finally
    {
        // dispose only if we own the stdout
        if (nextStdOut is null)
        {
            await (stdOut?.DisposeAsync() ?? ValueTask.CompletedTask);
        }

        await (stdErr?.DisposeAsync() ?? ValueTask.CompletedTask);
        stdInCaptureSub?.Dispose();
    }
}
