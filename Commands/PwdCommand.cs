namespace CodeCrafters.Shell.Commands;

public record PwdCommand() : Command("pwd")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        await ctx.StdOut.WriteLineAsync(Environment.CurrentDirectory);
    }
}