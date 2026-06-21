using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

public static class Extensions
{
    extension(Process p)
    {
        public async IAsyncEnumerable<ProcessOutputLine> ReadAllLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var outputReader = p.StandardOutput;
            var errorReader = p.StandardError;

            var channel = Channel.CreateBounded<ProcessOutputLine>(0);
            var firstCompleted = false;

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var outputTask = ReadToChannelAsync(outputReader, false, linkedCts.Token);
            var errorTask = ReadToChannelAsync(errorReader, true, linkedCts.Token);

            try
            {
                while (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (channel.Reader.TryRead(out var line))
                    {
                        yield return line;
                    }
                }
            }
            finally
            {
                linkedCts.Cancel();

                // Ensure both tasks complete before disposing the CancellationTokenSource.
                // The tasks handle all exceptions internally, so they always run to completion.
                await outputTask.ConfigureAwait(false);
                await errorTask.ConfigureAwait(false);

                linkedCts.Dispose();
            }

            async Task ReadToChannelAsync(StreamReader reader, bool standardError, CancellationToken ct)
            {
                try
                {
                    while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is string line)
                    {
                        await channel.Writer.WriteAsync(new ProcessOutputLine(line, standardError), ct).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    channel.Writer.TryComplete(ex);

                    return;
                }

                if (Interlocked.Exchange(ref firstCompleted, true))
                {
                    channel.Writer.TryComplete();
                }
            }
        }
    }
}

public record ProcessOutputLine(string Content, bool StandardError);