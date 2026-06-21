namespace CodeCrafters.Shell.Commands;

public record CdCommand() : Command("cd")
{
    public override async Task Handle(CommandExecutionContext ctx)
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
    }
}