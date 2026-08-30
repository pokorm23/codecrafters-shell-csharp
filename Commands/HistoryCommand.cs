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

            if (option == "-r" && file.Exists)
            {
                var historyToRead = await File.ReadAllLinesAsync(file.FullName);

                foreach (var se in historyToRead.Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    CommandHistoryStore.Commands.Add(se);
                }
            }

            if (option == "-w")
            {
                await using var stream = file.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                await using var writer = new StreamWriter(stream);

                foreach (var m in CommandHistoryStore.Commands)
                {
                    await writer.WriteLineAsync(m);
                }
            }

            if (option == "-a")
            {
                await using var stream = file.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                await using var writer = new StreamWriter(stream);

                var commands = CommandHistoryStore.Commands;

                if (CommandHistoryStore.LastAppendedIndex.HasValue)
                {
                    commands = commands.Skip(CommandHistoryStore.LastAppendedIndex.Value + 1).ToList();
                }

                foreach (var m in commands)
                {
                    await writer.WriteLineAsync(m);
                }

                CommandHistoryStore.LastAppendedIndex = commands.Count - 1;
            }
        }
    }
}
