using System.Collections.Frozen;
using System.Text.RegularExpressions;

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
    new ("echo", async ctx =>
    {
        var echoLine = string.Join(" ", ctx.Args);

        await ctx.StdOut.WriteLineAsync(echoLine);
    }),
    new ("type", async ctx =>
    {
        var what = ctx.Args.FirstOrDefault() ?? "";

        var c = ctx.GetCommand(what);

        if (c is null)
        {
            await ctx.StdOut.WriteLineAsync($"{what}: not found");
        }
        else if (c is ShellBuiltinCommand)
        {
            await ctx.StdOut.WriteLineAsync($"{what} is a shell builtin");
        }
        else if (c is PathFileCommand pf)
        {
            await ctx.StdOut.WriteLineAsync($"{what} is {pf.File.FullName}");
        }
        else
        {
            throw new NotImplementedException();
        }
    }),
    new ("pwd", async ctx =>
    {
        await ctx.StdOut.WriteLineAsync(Environment.CurrentDirectory);
    }),
    new ("cd", async ctx =>
    {
        var path = ctx.Args.FirstOrDefault() ?? "";

        if (path.StartsWith("~"))
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? "";
            path = $"{home}{path[1..]}";
        }

        if (!Directory.Exists(path))
        {
            await ctx.StdOut.WriteLineAsync($"cd: {path}: No such file or directory");

            return ;
        }

        Environment.CurrentDirectory = path;
    }),
};

while (!cts.Token.IsCancellationRequested)
{
    Console.Write("$ ");

    var userLine = await Console.In.ReadLineAsync();

    userLine = userLine?.Trim() ?? string.Empty;

    var allParsedArguments = CliParser.GetArgs(userLine);

    var argumentGroups = new List<(string? PipeOperator, string[] Args)>();
    var capture = new List<string>();
    var pipe = default(string);

    void CommitCapture()
    {
        argumentGroups.Add((pipe, capture.ToArray()));
        capture.Clear();
    }

    foreach (var argument in allParsedArguments)
    {
        if (argument is "||" or "&&" or ";" or "|")
        {
            CommitCapture();
            pipe = argument;
        }
        else
        {
            capture.Add(argument);
        }
    }

    CommitCapture();


    foreach (var (_, allArgs) in argumentGroups)
    {
        if (allArgs.Length is 0)
        {
            throw new Exception("not enought arguments");
        }

        var command = "";
        var arguments = new string[allArgs.Length - 1];
        var redirections = new Dictionary<int, (RedirectionType Type, string? Target)>();
        var locatingTarget = default(int?);

        foreach (var (i, a) in allArgs.Index())
        {
            if (i == 0)
            {
                command = a;

                continue;
            }

            if (RedirectionPart().Match(a) is { Success: true } m)
            {
                if (locatingTarget.HasValue)
                {
                    throw new Exception("missing target for previous redirect");
                }

                var (n, t) = (int.Parse(m.Groups["r"].Value), m.Groups["t"].Value switch
                                 {
                                     ">"   => RedirectionType.Override,
                                     ">>"  => RedirectionType.Append,
                                     var _ => throw new Exception($"Unknown redirect type {m.Groups["t"]}"),
                                 });

                if (!redirections.TryAdd(n, (t, null)))
                {
                    throw new Exception($"Multiple redirection for {n} in command {allArgs.ToCollString()}");
                }

                locatingTarget = n;

                continue;
            }

            if (locatingTarget is { } r)
            {
                redirections[r] = (redirections[r].Type, a);

                locatingTarget = null;

                continue;
            }
            
            if (locatingTarget.HasValue)
            {
                throw new Exception("missing target for previous redirect");
            }

            arguments[i - 1] = a;
        }

        var ctx = new CommandExecutionContext(arguments, wellKnownCommands)
        {
            CancellationToken = cts.Token,
            Redirections = redirections
                           .Select(x => (x.Key, (x.Value.Type, x.Value.Target ?? throw new Exception($"Missing target for redirection {x.Key} in {allArgs.ToCollString()}"))))
                           .ToFrozenDictionary(x=>x.Key,x=>x.Item2),
        };

        var foundCommand = ctx.GetCommand(command);

        if (foundCommand is null)
        {
            await ctx.StdOut.WriteLineAsync($"{command}: command not found");

            continue;
        }

        await foundCommand.Callback(ctx);

        if (ctx.IsHaltRequested)
        {
            return ;
        }
    }
}

internal partial class Program
{
    [GeneratedRegex(@"(?<r>\d*)(?<t>>|>>)")]
    private static partial Regex RedirectionPart();
}
