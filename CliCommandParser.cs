using System.Text.RegularExpressions;

namespace CodeCrafters.Shell;

public record CliRawCommand(string Command, string[] Args, Dictionary<int, (RedirectionType Type, string Target)> Redirections, bool IsBackground) { }

public static partial class CliCommandParser
{
    public static CliRawCommand ParseCommand(string[] allArgs)
    {
        if (allArgs.Length is 0)
        {
            throw new Exception("not enough arguments");
        }

        var command = "";
        var arguments = new List<string>();
        var redirections = new Dictionary<int, (RedirectionType Type, string? Target)>();
        var locatingTarget = default(int?);
        var isBg = false;

        foreach (var (i, a) in allArgs.Index())
        {
            if (i == 0)
            {
                command = a;

                continue;
            }

            if (a == "&" && !isBg && i == allArgs.Length - 1)
            {
                isBg = true;

                continue;
            }

            if (isBg)
            {
                throw new Exception("'&' should be only at the end");
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

        return new CliRawCommand(command, arguments.ToArray(), validatedRedirections, isBg);
    }

    [GeneratedRegex(@"^(?<r>\d*)(?<t>>|>>)$")]
    private static partial Regex RedirectionPart();
}
