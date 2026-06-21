namespace CodeCrafters.Shell.Commands;

public record EchoCommand() : Command("echo")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        var echoLine = string.Join(" ", ctx.Args);

        await ctx.StdOut.WriteLineAsync(echoLine);
    }
}