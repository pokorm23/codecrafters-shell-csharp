namespace CodeCrafters.Shell.Commands;

public record ExitCommand() : Command("exit")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        ctx.Halt();

        await CommandHistoryStore.Save();
    }
}