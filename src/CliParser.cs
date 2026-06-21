using System.Text;

public static class CliParser
{
    public const char TokenSplitChar = ' ';
    public const char SingleQuote = '\'';
    public const char DoubleQuote = '"';
    public const char EscapeChar = '\\';
    public const char DefaultChar = '\0';

    public static string[] GetArgs(string line) => GetTokens(line).ToArray();

    private static IEnumerable<string> GetTokens(string line)
    {
        var nextCharToEscape = true;
        var quotes = DefaultChar;
        var b = new StringBuilder();

        foreach (var c in line)
        {
            if (c == EscapeChar && c is not SingleQuote)
            {
                if (!nextCharToEscape)
                {
                    nextCharToEscape = true;
                }
                else
                {
                    b.Append(c);
                }
            }
            // space as token breaker
            else if (!nextCharToEscape && c == TokenSplitChar && quotes == DefaultChar)
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
                (quotes, var append) = (nextCharToEscape, quotes, c) switch
                {
                    (true, var q, _)           => (q, true),
                    (false, SingleQuote, SingleQuote) => (DefaultChar, false),
                    (false, DoubleQuote, DoubleQuote) => (DefaultChar, false),
                    (false, DefaultChar, SingleQuote) => (SingleQuote, false),
                    (false, DefaultChar, DoubleQuote) => (DoubleQuote, false),
                    (false, var q, _)           => (q, true),
                };

                if (append)
                {
                    b.Append(c);
                }

                if (nextCharToEscape)
                {
                    nextCharToEscape = false;
                }
            }
        }

        if (b.Length > 0)
        {
            yield return b.ToString();
        }
    }
}
