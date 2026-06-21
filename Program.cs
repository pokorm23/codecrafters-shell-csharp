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
    Console.Write("$ ");

    var userLine = await Console.In.ReadLineAsync();

    userLine = userLine?.Trim() ?? string.Empty;

    var allParsedArguments = CliTokenParser.GetTokens(userLine);

    foreach (var (_, allArgs) in CliPipeParser.GetCommands(allParsedArguments.ToArray()))
    {
        CliRawCommand rawCommand;

        try
        {
            rawCommand = CliCommandParser.ParseCommand(allArgs);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error while parsing {allArgs.ToCollString()}: {e.Message}");

            throw;
        }

        var ctx = await RunCommand(rawCommand, cts.Token);

        if (ctx.IsHaltRequested)
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
};

static async Task<CommandExecutionContext> RunCommand(CliRawCommand rawCommand, CancellationToken cancellationToken)
{
    TextWriter? stdOut = null;
    TextWriter? stdErr = null;

    try
    {
        if (rawCommand.Redirections.TryGetValue(1, out var re))
        {
            stdOut = new StreamWriter(File.Open(re.Target, re.Type switch
            {
                RedirectionType.Append => FileMode.Append,
                var _                  => FileMode.Create,
            }, FileAccess.Write));
        }

        if (rawCommand.Redirections.TryGetValue(2, out var se))
        {
            stdErr = new StreamWriter(File.Open(se.Target, se.Type switch
            {
                RedirectionType.Append => FileMode.Append,
                var _                  => FileMode.Create,
            }, FileAccess.Write));
        }

        var ctx = new CommandExecutionContext(rawCommand.Args.ToArray(), GetWellKnownCommands())
        {
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
