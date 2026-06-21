using System.Text;

public static class CliParser
{
    public const char TokenSplitChar = ' ';
    public const char SingleQuote = '\'';

    public static string[] GetArgs(string line) => GetTokens(line).Select(Unquote).ToArray();

    private static IEnumerable<string> GetTokens(string line)
    {
        var lastChar = default(char);
        var quotes = false;
        var b = new StringBuilder();

        foreach (var c in line)
        {
            if (c == TokenSplitChar && !quotes)
            {
                if (b.Length == 0)
                {
                    continue;
                }

                yield return b.ToString();

                b.Clear();
            }
            else
            {
                b.Append(c);

                if (c == SingleQuote)
                {
                    quotes = !quotes;
                }
            }
        }

        if (b.Length > 0)
        {
            yield return b.ToString();
        }
    }

    private static string Unquote(string token)
    {
        var b = new StringBuilder();

        foreach (var c in token)
        {
            if (c != SingleQuote)
            {
                b.Append(c);
            }
        }

        return b.ToString();
    }
}
