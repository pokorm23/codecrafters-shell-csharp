var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

while (!cts.Token.IsCancellationRequested)
{
    Console.Write("$ ");

    var command = await Console.In.ReadLineAsync();

    command = command?.Trim() ?? string.Empty;

    if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    Console.WriteLine($"{command}: command not found");
}
