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
    
    Console.Write("$ ");

    var userLine = await Console.In.ReadLineAsync();

    userLine = userLine?.Trim() ?? string.Empty;

    var allParsedArguments = CliTokenParser.GetTokens(userLine);

    foreach (var (_, allArgs) in CliPipeParser.GetCommandGroups(allParsedArguments.ToArray()))
    {
        CommandExecutionContext? pipePrevContext = null;

        var pipe = CliPipeParser.GetCommandPipe(allArgs);

        var isPipe = pipe.Count > 1;
        
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
            
            CommandExecutionContext ctx;
            
            // if is pipe
            if (i > 0)
            {
                ctx = await RunCommand(rawCommand, cts.Token);
            }
            else
            {
                ctx = await RunCommand(rawCommand, cts.Token);
            }


            if (ctx.IsHaltRequested)
            {
                return;
            }

            pipePrevContext = ctx;
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
};

static async Task<CommandExecutionContext> RunCommand(CliRawCommand rawCommand, TextWriter? nextStdOut,TextReader? previousStdIn, CancellationToken cancellationToken)
{
    TextWriter? stdOut = null;
    TextWriter? stdErr = null;

    try
    {
        if (rawCommand.Redirections.TryGetValue(1, out var re))
        {
            if (re.Type == RedirectionType.Pipe)
            {
                stdOut = nextStdOut;
            }
            else
            {
                stdOut = new StreamWriter(File.Open(re.Target, re.Type switch
                {
                    RedirectionType.Append   => FileMode.Append,
                    RedirectionType.Override => FileMode.Create,
                    var _                    => throw new ArgumentOutOfRangeException(),
                }, FileAccess.Write));
            }
        }

        if (rawCommand.Redirections.TryGetValue(2, out var se))
        {
            stdErr = new StreamWriter(File.Open(se.Target, se.Type switch
            {
                RedirectionType.Append   => FileMode.Append,
                RedirectionType.Override => FileMode.Create,
                _                        => throw new ArgumentOutOfRangeException()
            }, FileAccess.Write));
        }

        var ctx = new CommandExecutionContext(rawCommand.Args.ToArray(), GetWellKnownCommands())
        {
            RawCommand = rawCommand,
            CancellationToken = cancellationToken,
            StdOut = stdOut ?? Console.Out,
            StdErr = stdErr ?? Console.Error,
            InBackground = rawCommand.IsBackground,
        };

        var foundCommand = ctx.GetCommand(rawCommand.Command);

        if (foundCommand is null)
        {
            await ctx.StdOut.WriteLineAsync($"{rawCommand.Command}: command not found");

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
        await (stdOut?.DisposeAsync() ?? ValueTask.CompletedTask);
        await (stdErr?.DisposeAsync() ?? ValueTask.CompletedTask);
    }
}
