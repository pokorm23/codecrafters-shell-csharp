namespace CodeCrafters.Shell.Commands;

public record TypeCommand() : Command("exit")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        var what = ctx.Args.FirstOrDefault() ?? "";

        var c = ctx.GetCommand(what);

        if (c is null)
        {
            await ctx.StdOut.WriteLineAsync($"{what}: not found");
        }
        else if (c is PathFileCommand pf)
        {
            await ctx.StdOut.WriteLineAsync($"{what} is {pf.File.FullName}");
        }
        else
        {
            // assumes shell
            await ctx.StdOut.WriteLineAsync($"{what} is a shell builtin");
        }
    }
}