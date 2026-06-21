namespace CodeCrafters.Shell.Commands;

public record ExitCommand() : Command("exit")
{
    public override Task Handle(CommandExecutionContext ctx)
    {
        ctx.Halt();

        return Task.CompletedTask;
    }
}