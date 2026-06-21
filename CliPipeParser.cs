namespace CodeCrafters.Shell;

public static class CliPipeParser
{
    public static List<(string? PipeOperator, string[] Args)> GetCommands(string[] tokens)
    {
        var argumentGroups = new List<(string? PipeOperator, string[] Args)>();
        var capture = new List<string>();
        var pipe = default(string);

        foreach (var argument in tokens)
        {
            if (argument is "||" or "&&" or ";" or "|")
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
}
