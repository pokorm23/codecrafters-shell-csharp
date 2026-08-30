namespace CodeCrafters.Shell;

public record ProcessDescriptor(Func<int?> Pid, Task ExitTask, Func<bool> Exited, Func<TextWriter?> StdIn);