namespace CodeCrafters.Shell;

public static class CommandHistoryStore
{
    public static List<string> Commands { get; } = [];

    public static int? LastAppendedIndex = null;
}
