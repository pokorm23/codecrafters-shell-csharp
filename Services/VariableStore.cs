namespace CodeCrafters.Shell;

public static class VariableStore
{
    public static Dictionary<string, string> Variables = [];

    public static bool Set(string name, string value)
    {
        if (value.Length > 0)
        {
            if (!char.IsAsciiLetter(value[0]) || value[0] != '_')
            {
                return false;
            }

            if (value.Length > 1 && value[1..].Any(c => !char.IsAsciiLetterOrDigit(c) || c != '_'))
            {
                return false;
            }
        }

        Variables[name] = value;

        return true;
    }

    public static string? Get(string varName) => Variables[varName];
}
