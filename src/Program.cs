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

            return;
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
        try
        {
            if (allArgs.Length is 0)
            {
                throw new Exception("not enought arguments");
            }

            var command = "";
            var arguments = new List<string>();
            var redirections = new Dictionary<int, (RedirectionType Type, string? Target)>();
            var locatingTarget = default(int?);

            foreach (var (i, a) in allArgs.Index())
            {
                if (i == 0)
                {
                    command = a;

                    continue;
                }

                if (RedirectionPart().Match(a) is {Success: true} m)
                {
                    if (locatingTarget.HasValue)
                    {
                        throw new Exception("missing target for previous redirect");
                    }

                    var (gr, gt) = (m.Groups["r"].Value, m.Groups["t"].Value);

                    var (n, t) = (string.IsNullOrWhiteSpace(gr) ? 1 : int.Parse(gr), gt switch
                                     {
                                         ">"   => RedirectionType.Override,
                                         ">>"  => RedirectionType.Append,
                                         var _ => throw new Exception($"Unknown redirect type {gt}"),
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

                arguments.Add(a);
            }

            var validatedRedirections = redirections
                                        .Select(x => (x.Key, (x.Value.Type, Target: x.Value.Target ?? throw new Exception($"Missing target for redirection {x.Key} in {allArgs.ToCollString()}"))))
                                        .ToDictionary();

            TextWriter? stdOut = null;

            if (validatedRedirections.TryGetValue(1, out var re))
            {
                stdOut = new StreamWriter(File.Open(re.Target, re.Type switch
                {
                    RedirectionType.Append => FileMode.Append,
                    _                      => FileMode.Create,
                }, FileAccess.Write));
            }
            
            TextWriter? stdErr = null;

            if (validatedRedirections.TryGetValue(2, out var se))
            {
                stdErr = new StreamWriter(File.Open(se.Target, se.Type switch
                {
                    RedirectionType.Append => FileMode.Append,
                    _                      => FileMode.Create,
                }, FileAccess.Write));
            }

            var ctx = new CommandExecutionContext(arguments.ToArray(), wellKnownCommands)
            {
                CancellationToken = cts.Token,
                StdOut = stdOut ?? Console.Out,
                StdErr = stdErr ?? Console.Error,
            };

            var foundCommand = ctx.GetCommand(command);

            if (foundCommand is null)
            {
                await ctx.StdOut.WriteLineAsync($"{command}: command not found");

                continue;
            }

            await foundCommand.Callback(ctx);

            await (stdOut?.DisposeAsync() ?? ValueTask.CompletedTask);
            await (stdErr?.DisposeAsync() ?? ValueTask.CompletedTask);

            if (ctx.IsHaltRequested)
            {
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error while parsing {allArgs.ToCollString()}: {e.Message}");

            throw;
        }
    }
}

internal partial class Program
{
    [GeneratedRegex(@"(?<r>\d*)(?<t>>|>>)")]
    private static partial Regex RedirectionPart();
}
