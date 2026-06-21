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

    Console.WriteLine($"{command}: command not found");
}
