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

    var arguments = userLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    (var command, arguments) = arguments is [var c, ..var a] ? (c, a) : (string.Empty, []);

    if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    else if (command.Equals("echo", StringComparison.OrdinalIgnoreCase))
    {
        var echoLine = string.Join(" ", arguments);

        Console.WriteLine(echoLine);
    }
    else
    {
        Console.WriteLine($"{command}: command not found");
    }
}
