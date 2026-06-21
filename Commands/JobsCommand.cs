namespace CodeCrafters.Shell.Commands;

public record JobsCommand() : Command("jobs")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        var jobs = BackgroundJobStorage.Jobs.ToDictionary();

        foreach (var (i, (number, (command, p))) in jobs.OrderBy(x => x.Key).Index())
        {
            var exited = p.Exited();

            var status = exited ? "Done" : "Running";

            status = $" {status}".PadRight(24, ' ');

            var lastness = i == jobs.Count - 1
                               ? "+"
                               : i == jobs.Count - 2
                                   ? "-" : "";

            await ctx.StdOut.WriteLineAsync($"[{number}]{lastness} {status}{command.OriginalInput}");

            if (exited)
            {
                BackgroundJobStorage.Jobs.TryRemove(number, out _);
            }
        }
    }
}
