using System.Text;

public static class CliParser
{
    public const char TokenSplitChar = ' ';
    public const char SingleQuote = '\'';
    public const char DoubleQuote = '"';
    public const char DefaultChar = '\0';

    public static string[] GetArgs(string line) => GetTokens(line).Select(Unquote).ToArray();

    private static IEnumerable<string> GetTokens(string line)
    {
        var quotes = DefaultChar;
        var b = new StringBuilder();

        foreach (var c in line)
        {
            if (c == TokenSplitChar && quotes == DefaultChar)
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

                quotes = (quotes, c) switch
                {
                    (SingleQuote, SingleQuote) => DefaultChar,
                    (DoubleQuote, DoubleQuote) => DefaultChar,
                    (DefaultChar, SingleQuote) => SingleQuote,
                    (DefaultChar, DoubleQuote) => DoubleQuote,
                    var (q, _)                 => q,
                };
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
            if (c is not SingleQuote or DoubleQuote)
            {
                b.Append(c);
            }
        }

        return b.ToString();
    }
}
