namespace CodeCrafters.Shell;

public static class CliPipeParser
{
    public static List<(string? PipeOperator, string[] Args)> GetCommandGroups(string[] tokens)
    {
        var argumentGroups = new List<(string? PipeOperator, string[] Args)>();
        var capture = new List<string>();
        var pipe = default(string);

        foreach (var argument in tokens)
        {
            if (argument is "||" or "&&" or ";")
            {
                CommitCapture();
                pipe = argument;
            }
            else
            {
                capture.Add(argument);
            }
        }

        CommitCapture();

        return argumentGroups;

        void CommitCapture()
        {
            argumentGroups.Add((pipe, capture.ToArray()));
            capture.Clear();
        }
    }

    public static IReadOnlyCollection<string[]> GetCommandPipe(string[] tokens)
    {
        var argumentGroups = new List<string[]>();
        var capture = new List<string>();

        foreach (var argument in tokens)
        {
            if (argument is "|")
            {
                CommitCapture();
            }
            else
            {
                capture.Add(argument);
            }
        }

        CommitCapture();

        return argumentGroups;

        void CommitCapture()
        {
            argumentGroups.Add(capture.ToArray());
            capture.Clear();
        }
    }
}
