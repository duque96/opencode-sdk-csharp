using System.Runtime.CompilerServices;
using System.Text;

namespace Opencode.Internal.Streaming;

internal static class SseMessageParser
{
    public static async IAsyncEnumerable<ServerSentEvent> ParseAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

        string? eventName = null;
        List<string> dataLines = [];
        List<string> rawLines = [];

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                if (eventName is not null || dataLines.Count > 0)
                {
                    yield return CreateMessage(eventName, dataLines, rawLines);
                }

                yield break;
            }

            if (line.Length == 0)
            {
                if (eventName is null && dataLines.Count == 0)
                {
                    rawLines.Clear();
                    continue;
                }

                yield return CreateMessage(eventName, dataLines, rawLines);
                eventName = null;
                dataLines = [];
                rawLines = [];
                continue;
            }

            rawLines.Add(line);

            if (line[0] == ':')
            {
                continue;
            }

            var separatorIndex = line.IndexOf(':');
            var fieldName = separatorIndex >= 0 ? line[..separatorIndex] : line;
            var value = separatorIndex >= 0 ? line[(separatorIndex + 1)..] : string.Empty;

            if (value.StartsWith(' '))
            {
                value = value[1..];
            }

            if (fieldName.Equals("event", StringComparison.Ordinal))
            {
                eventName = value;
            }
            else if (fieldName.Equals("data", StringComparison.Ordinal))
            {
                dataLines.Add(value);
            }
        }
    }

    private static ServerSentEvent CreateMessage(string? eventName, List<string> dataLines, List<string> rawLines)
    {
        return new ServerSentEvent
        {
            EventName = eventName,
            Data = string.Join('\n', dataLines),
            RawLines = rawLines.ToArray(),
        };
    }
}