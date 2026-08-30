namespace CodeCrafters.Shell;

public static class VariableStore
{
    public static Dictionary<string, string> Variables = [];

    public static bool Set(string name, string value)
    {
        if (name.Length > 0)
        {
            if (!char.IsAsciiLetter(name[0]) && name[0] != '_')
            {
                return false;
            }

            if (name.Length > 1 && name[1..].Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_'))
            {
                return false;
            }
        }

        Variables[name] = value;

        return true;
    }

    public static string? Get(string varName)
    {
        return Variables.GetValueOrDefault(varName);
    }
}
