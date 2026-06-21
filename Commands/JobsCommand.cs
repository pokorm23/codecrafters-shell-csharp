namespace CodeCrafters.Shell.Commands;

public record JobsCommand() : Command("jobs")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        var jobs = BackgroundJobStorage.Jobs.ToDictionary();

        foreach (var (i,(number,command)) in jobs.Order().Index())
        {
            var status = $" Running".PadRight(24, ' ');

            var lastness = i == jobs.Count - 1
                               ? "+"
                               : i == jobs.Count - 2
                                   ? "-" : "";

            await ctx.StdOut.WriteLineAsync($"[{number}]{lastness} {status}{command.OriginalInput}");
        }
    }
}