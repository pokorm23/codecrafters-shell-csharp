namespace CodeCrafters.Shell;

public abstract record Command(string CommandName)
{
    public abstract Task Handle(CommandExecutionContext context);
}