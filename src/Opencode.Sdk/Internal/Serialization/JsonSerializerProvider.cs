using System.Text.Json;

namespace Opencode.Internal.Serialization;

internal sealed class JsonSerializerProvider
{
    public JsonSerializerProvider(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options;
    }

    public JsonSerializerOptions Options { get; }
}