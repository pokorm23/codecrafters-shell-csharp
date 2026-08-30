using System.Collections.Concurrent;

namespace CodeCrafters.Shell;

public static class BackgroundJobStorage
{
    public static ConcurrentDictionary<int, (CliRawCommand Command, ProcessDescriptor Process)> Jobs { get; } = [];

    public static async Task WriteAndReap(TextWriter writer, bool skipRunning)
    {
        var jobs = Jobs.ToDictionary();

        foreach (var (i, (number, (command, p))) in jobs.OrderBy(x => x.Key).Index())
        {
            var exited = p.Exited();

            if (!exited && skipRunning)
            {
                continue;
            }

            var status = exited ? "Done" : "Running";

            status = $" {status}".PadRight(24, ' ');

            var lastness = i == jobs.Count - 1
                               ? "+"
                               : i == jobs.Count - 2
                                   ? "-" : " ";

            await writer.WriteLineAsync($"[{number}]{lastness} {status}{command.OriginalInput.TrimEnd('&').Trim()}");

            if (exited)
            {
                Jobs.TryRemove(number, out var _);
            }
        }
    }
}
