public abstract record Command(string CommandName, CommandType Type, Func<CommandExecutionContext, Task> Callback);
public record ShellBuiltinCommand(string CommandName, Func<CommandExecutionContext, Task> Callback) : Command(CommandName, CommandType.ShellBuilin, Callback);
public record PathFileCommand(FileInfo File, Func<CommandExecutionContext, Task> Callback) : Command(File.Name, CommandType.PathFile, Callback);
