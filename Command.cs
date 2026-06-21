namespace CodeCrafters.Shell;

public abstract record Command(string CommandName)
{
    public abstract Task Handle(CommandExecutionContext context);
}

public record PathFileCommand(FileInfo File, Func<CommandExecutionContext, Task> Callback) : Command(File.Name)
{
    public override Task Handle(CommandExecutionContext context) => this.Callback(context);
}