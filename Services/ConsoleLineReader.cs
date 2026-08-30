namespace CodeCrafters.Shell;

public record ConsoleLineResult(string Text, bool IsCancelRequested);

public static class ConsoleLineReader
{
    public static ConsoleLineResult GetPrompt()
    {
        Console.Out.Write("$ ");

        int? historyPointer = null;
        var history = CommandHistoryStore.Commands.Index().ToDictionary(x => x.Index, x => x.Item);

        var userLine = "";
        var typedPrompt = "";

        while (true)
        {
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }
            else if (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                return new ConsoleLineResult("", true);
            }
            else if (key.Key == ConsoleKey.UpArrow)
            {
                if (historyPointer == 0 || history.Count == 0)
                {
                    continue;
                }

                if (!historyPointer.HasValue)
                {
                    historyPointer = history.Count - 1;
                }
                else
                {
                    historyPointer--;
                }

                userLine = historyPointer is { } p
                               ? RedrawPrompt(history[p], userLine)
                               : RedrawPrompt(typedPrompt, userLine);
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                if (!historyPointer.HasValue)
                {
                    continue;
                }

                if (historyPointer == history.Count - 1)
                {
                    historyPointer = null;
                }
                else
                {
                    historyPointer++;
                }

                userLine = historyPointer is { } p
                               ? RedrawPrompt(history[p], userLine)
                               : RedrawPrompt(typedPrompt, userLine);
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (userLine.Length > 0)
                {
                    userLine = userLine[..^1];

                    Console.Out.Write("\b \b");
                }

                if (!historyPointer.HasValue)
                {
                    typedPrompt = userLine;
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                Console.Out.Write(key.KeyChar);
                userLine += key.KeyChar;

                if (!historyPointer.HasValue)
                {
                    typedPrompt = userLine;
                }
            }
        }

        CommandHistoryStore.Commands.Add(userLine);

        Console.Out.WriteLine();

        return new ConsoleLineResult(userLine, false);
    }

    private static string RedrawPrompt(string text, string currentText)
    {
        foreach (var _ in currentText)
        {
            Console.Out.Write("\b \b");
        }

        Console.Out.Write(text);

        return text;
    }
}
