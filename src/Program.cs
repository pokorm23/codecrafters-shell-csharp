var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var wellKnownCommands = new List<ShellBuiltinCommand>
{
    new ("exit", ctx =>
    {
        ctx.Halt();

        return Task.CompletedTask;
    }),
    new ("echo", ctx =>
    {
        var echoLine = string.Join(" ", ctx.Args.Skip(1));

        Console.WriteLine(echoLine);

        return Task.CompletedTask;
    }),
    new ("type", ctx =>
    {
        var what = ctx.Args.Skip(1).FirstOrDefault() ?? "";

        var c = ctx.GetCommand(what);

        if (c is null)
        {
            Console.WriteLine($"{what}: not found");
        }
        else if (c is ShellBuiltinCommand)
        {
            Console.WriteLine($"{what} is a shell builtin");
        }
        else if (c is PathFileCommand pf)
        {
            Console.WriteLine($"{what} is {pf.File.FullName}");
        }
        else
        {
            throw new NotImplementedException();
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

    (var command, _) = arguments is [var c, ..var a] ? (c, a) : (string.Empty, []);

    var ctx = new CommandExecutionContext(arguments, wellKnownCommands);

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

