namespace CodeCrafters.Shell.Commands;

public record HistoryCommand() : Command("history")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        foreach (var (i, m) in CommandHistoryStore.Commands.Index())
        {
            await ctx.StdOut.WriteLineAsync($"    {i+1} {m}");
        }
    }
}
