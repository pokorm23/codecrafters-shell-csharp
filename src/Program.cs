var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var commands = new List<Command>
{
    new ("exit", CommandType.ShellBuilin, ctx =>
    {
        ctx.Halt();

        return Task.CompletedTask;
    }),
    new ("echo", CommandType.ShellBuilin, ctx =>
    {
        var echoLine = string.Join(" ", ctx.Args);

        Console.WriteLine(echoLine);

        return Task.CompletedTask;
    }),
    new ("type", CommandType.ShellBuilin, ctx =>
    {
        var what = ctx.Args.FirstOrDefault() ?? "";

        var c = ctx.GetCommand(what);

        if (c is null)
        {
            Console.WriteLine($"{what}: not found");
        }
        else
        {
            Console.WriteLine(c.Type.GetDescription(what));
        }

        return Task.CompletedTask;
    }),
};

while (!cts.Token.IsCancellationRequested)
{
    Console.Write("$ ");

    var userLine = await Console.In.ReadLineAsync();

    userLine = userLine?.Trim() ?? string.Empty;

    var arguments = userLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    (var command, arguments) = arguments is [var c, ..var a] ? (c, a) : (string.Empty, []);

    var ctx = new CommandExecutionContext(arguments, commands);

    var foundCommand = ctx.GetCommand(command);

    if (foundCommand is null)
    {
        Console.WriteLine($"{command}: command not found");

        continue;
    }

    await foundCommand.Callback(ctx);

    if (ctx.IsHaltRequested)
    {
        break;
    }
}

public record Command(string CommandName, CommandType Type, Func<CommandExecutionContext, Task> Callback);

public record CommandExecutionContext(string[] Args, IReadOnlyCollection<Command> AllCommands)
{
    public bool IsHaltRequested { get; private set; }

    public Command? GetCommand(string command)
    {
        return this.AllCommands.FirstOrDefault(x => command.Equals(x.CommandName, StringComparison.OrdinalIgnoreCase));
    }

    public void Halt()
    {
        this.IsHaltRequested = true;
    }
}

public enum CommandType
{
    ShellBuilin,
}

public static class DescriptionExtensions
{
    extension(CommandType type)
    {
        public string GetDescription(string command) => type switch
        {
            CommandType.ShellBuilin => $"{command} is a shell buildin",
        };
    }
}
