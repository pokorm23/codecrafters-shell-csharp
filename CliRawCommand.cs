namespace CodeCrafters.Shell;

public record CliRawCommand(string Command, string[] Args, Dictionary<int, (RedirectionType Type, string Target)> Redirections, bool IsBackground)
{
    public required string OriginalInput { get; init; }
}