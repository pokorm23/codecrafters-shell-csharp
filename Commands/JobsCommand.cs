namespace CodeCrafters.Shell.Commands;

public record JobsCommand() : Command("jobs")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        await BackgroundJobStorage.WriteAndReap(ctx.StdOut, false);
    }
}
