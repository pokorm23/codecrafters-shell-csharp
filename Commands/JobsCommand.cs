namespace CodeCrafters.Shell.Commands;

public record JobsCommand() : Command("jobs")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        foreach (var (command,_) in BackgroundJobStorage.Jobs)
        {
            var status = $" Running".PadRight(24, ' ');

            await ctx.StdOut.WriteLineAsync($"[1]+ {status}{command.OriginalInput}");
        }
    }
}