namespace CodeCrafters.Shell.Commands;

public record HistoryCommand() : Command("history")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        int? n = int.TryParse(ctx.Args.FirstOrDefault() ?? "", out var ii) ? ii : null;
        
        var toTake = !n.HasValue
                     || n.Value > CommandHistoryStore.Commands.Count
                     || n.Value < 0
                     ? CommandHistoryStore.Commands.Count
                     : n.Value;
        
        foreach (var (i, m) in CommandHistoryStore.Commands.Index().TakeLast(toTake))
        {
            await ctx.StdOut.WriteLineAsync($"    {i+1} {m}");
        }
    }
}
