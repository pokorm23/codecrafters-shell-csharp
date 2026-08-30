namespace CodeCrafters.Shell.Commands;

public record DeclareCommand() : Command("declare")
{
    public override async Task Handle(CommandExecutionContext ctx)
    {
        if (ctx.Args.Length == 1)
        {
            var arg = ctx.Args[0];

            var parts = arg.Split('=');

            if (parts.Length == 2)
            {
                var name = parts[0];
                var value = parts[1];

                VariableStore.Set(name, value);
            }
        }

        if (ctx.Args.Length == 2)
        {
            var option = ctx.Args[0];
            var varName = ctx.Args[1];

            if (option == "-p")
            {
                var variable = VariableStore.Get(varName);

                if (variable is null)
                {
                    await ctx.StdOut.WriteLineAsync($"declare: {varName}: not found");
                }
                else
                {
                    await ctx.StdOut.WriteLineAsync($"declare -- {varName}=\"{variable}\"");
                }
            }
        }
    }
}
