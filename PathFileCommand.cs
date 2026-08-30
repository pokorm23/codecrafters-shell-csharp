namespace CodeCrafters.Shell;

public record PathFileCommand(FileInfo File, Func<CommandExecutionContext, Task> Callback) : Command(File.Name)
{
    public override Task Handle(CommandExecutionContext context) => this.Callback(context);
}