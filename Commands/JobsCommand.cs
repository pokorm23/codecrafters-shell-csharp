namespace CodeCrafters.Shell.Commands;

public record JobsCommand() : Command("jobs")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        var jobs = BackgroundJobStorage.Jobs.ToDictionary();

        foreach (var (number,command) in jobs.Order())
        {
            var status = $" Running".PadRight(24, ' ');

            var lastness = number == jobs.Count - 1
                               ? "+"
                               : number == jobs.Count - 2
                                   ? "-" : "";

            await ctx.StdOut.WriteLineAsync($"[{number}]{lastness} {status}{command.OriginalInput}");
        }
    }
}