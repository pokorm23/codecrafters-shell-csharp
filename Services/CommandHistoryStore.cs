namespace CodeCrafters.Shell;

public static class CommandHistoryStore
{
    public static List<string> Commands { get; } = [];

    public static int? LastAppendedIndex = null;

    public static async Task Load()
    {
        var filePath = Environment.GetEnvironmentVariable("HISTFILE");

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var file = new FileInfo(filePath);

        await Load(file);
    }

    public static async Task Load(FileInfo file)
    {
        if (file.Exists)
        {
            var historyToRead = await File.ReadAllLinesAsync(file.FullName);

            foreach (var se in historyToRead.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                Commands.Add(se);
            }
        }
    }

    public static async Task Save()
    {
        var filePath = Environment.GetEnvironmentVariable("HISTFILE");

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var file = new FileInfo(filePath);

        await Save(file);
    }

    public static async Task Save(FileInfo file)
    {
        await using var stream = file.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(stream);

        foreach (var m in Commands)
        {
            await writer.WriteLineAsync(m);
        }
    }

    public static async Task Append()
    {
        var filePath = Environment.GetEnvironmentVariable("HISTFILE");

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var file = new FileInfo(filePath);

        await Append(file);
    }

    public static async Task Append(FileInfo file)
    {
        await using var stream = file.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(stream);

        var commands = Commands;

        if (LastAppendedIndex.HasValue)
        {
            commands = commands.Skip(LastAppendedIndex.Value + 1).ToList();
        }

        foreach (var m in commands)
        {
            await writer.WriteLineAsync(m);
        }

        LastAppendedIndex = commands.Count - 1;
    }
}
