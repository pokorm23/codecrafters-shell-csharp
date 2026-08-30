namespace CodeCrafters.Shell.Commands;

public record HistoryCommand() : Command("history")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        if (ctx.Args.Length is 0 or 1)
        {
            int? n = int.TryParse(ctx.Args.FirstOrDefault() ?? "", out var ii) ? ii : null;

            var toTake = !n.HasValue
                         || n.Value > CommandHistoryStore.Commands.Count
                         || n.Value < 0
                             ? CommandHistoryStore.Commands.Count
                             : n.Value;

            foreach (var (i, m) in CommandHistoryStore.Commands.Index().TakeLast(toTake))
            {
                await ctx.StdOut.WriteLineAsync($"    {i + 1} {m}");
            }
        }

        if (ctx.Args.Length == 2)
        {
            var option = ctx.Args[0].Trim();
            var file = new FileInfo(ctx.Args[1].Trim());

            if (option == "-r")
            {
                await CommandHistoryStore.Load(file);
            }

            if (option == "-w")
            {
                await CommandHistoryStore.Save(file);
            }

            if (option == "-a")
            {
                await CommandHistoryStore.Append(file);
            }
        }
    }
}
