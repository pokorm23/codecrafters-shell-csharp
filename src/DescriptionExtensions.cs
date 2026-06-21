public static class DescriptionExtensions
{
    extension(CommandType type)
    {
        public string GetDescription(string command) => type switch
        {
            CommandType.ShellBuilin => $"{command} is a shell builtin",
        };
    }
}