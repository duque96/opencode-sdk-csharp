using Opencode.Client;
using Opencode.Errors;
using Opencode.Models.Events;
using Opencode.Models.Session;

return await ProgramEntry.RunAsync(args);

internal static class ProgramEntry
{
    public static async Task<int> RunAsync(string[] args)
    {
        var scenario = args.FirstOrDefault()?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(scenario))
        {
            PrintUsage();
            return 1;
        }

        var client = CreateClient();

        try
        {
            switch (scenario)
            {
                case "chat":
                    await RunChatScenarioAsync(client, args.Skip(1).ToArray());
                    return 0;
                case "config":
                    await RunConfigScenarioAsync(client);
                    return 0;
                case "session":
                    await RunSessionScenarioAsync(client);
                    return 0;
                case "events":
                    await RunEventScenarioAsync(client);
                    return 0;
                case "resilience":
                    await RunResilienceScenarioAsync();
                    return 0;
                default:
                    Console.Error.WriteLine($"Unknown scenario '{scenario}'.");
                    PrintUsage();
                    return 1;
            }
        }
        catch (ApiException exception)
        {
            Console.Error.WriteLine($"SDK request failed: {exception.Message}");

            if (!string.IsNullOrWhiteSpace(exception.RequestId))
            {
                Console.Error.WriteLine($"Request ID: {exception.RequestId}");
            }

            if (!string.IsNullOrWhiteSpace(exception.ResponseBody))
            {
                Console.Error.WriteLine(exception.ResponseBody);
            }

            return 2;
        }
        catch (OpencodeException exception)
        {
            Console.Error.WriteLine($"SDK request failed: {exception.Message}");
            return 2;
        }
    }

    private static OpencodeClient CreateClient(TimeSpan? timeout = null)
    {
        var options = new OpencodeClientOptions
        {
            BaseUrl = new Uri(Environment.GetEnvironmentVariable("OPENCODE_BASE_URL") ?? "http://localhost:54321", UriKind.Absolute),
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
            MaxRetries = 2,
        };

        options.DefaultHeaders["X-Sample-Name"] = "sdk-samples";
        options.DiagnosticsHandler = static diagnosticEvent =>
            Console.Error.WriteLine($"[{diagnosticEvent.Kind}] {diagnosticEvent.Method} {diagnosticEvent.Path} attempt={diagnosticEvent.Attempt}");

        return new OpencodeClient(options);
    }

    private static async Task RunConfigScenarioAsync(OpencodeClient client)
    {
        var config = await client.Config.GetAsync();
        Console.WriteLine("Config request completed.");
        Console.WriteLine(config is null ? "No configuration payload returned." : "Configuration payload received.");
    }

    private static async Task RunChatScenarioAsync(OpencodeClient client, string[] args)
    {
        var modelId = GetRequiredEnvironmentVariable("OPENCODE_MODEL_ID");
        var providerId = GetRequiredEnvironmentVariable("OPENCODE_PROVIDER_ID");
        var prompt = ResolveChatPrompt(args);

        var session = await client.Session.CreateAsync()
            ?? throw new InvalidOperationException("The service returned no session payload.");

        Console.WriteLine($"Created session: {session.Id}");
        Console.WriteLine($"Prompt: {prompt}");

        var assistantMessage = await client.Session.ChatAsync(session.Id, CreateChatRequest(modelId, providerId, prompt));

        if (assistantMessage is not null)
        {
            Console.WriteLine($"Assistant reply ID: {assistantMessage.Id}");
        }

        var assistantTexts = await WaitForAssistantReplyTextsAsync(client, session.Id);

        if (assistantTexts.Count == 0)
        {
            Console.WriteLine("No assistant messages were returned in the session timeline.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Assistant reply:");

        foreach (var text in assistantTexts)
        {
            Console.WriteLine(text);
        }
    }

    private static async Task RunSessionScenarioAsync(OpencodeClient client)
    {
        var modelId = GetRequiredEnvironmentVariable("OPENCODE_MODEL_ID");
        var providerId = GetRequiredEnvironmentVariable("OPENCODE_PROVIDER_ID");

        var session = await client.Session.CreateAsync()
            ?? throw new InvalidOperationException("The service returned no session payload.");

        Console.WriteLine($"Created session: {session.Id}");

        await client.Session.InitAsync(session.Id, new SessionInitRequest
        {
            MessageId = "bootstrap",
            ModelId = modelId,
            ProviderId = providerId,
        });

        await client.Session.ChatAsync(
            session.Id,
            CreateChatRequest(
                modelId,
                providerId,
                "Summarize the current project goals in one sentence.",
                new Dictionary<string, bool>
                {
                    ["grep"] = true,
                }));

        var messages = await client.Session.MessagesAsync(session.Id) ?? [];
        Console.WriteLine($"Loaded {messages.Length} messages.");

        foreach (var message in messages)
        {
            Console.WriteLine($"- {message.Info.MessageRole}: {message.Info.Id} ({message.Parts.Count} parts)");
        }
    }

    private static async Task RunEventScenarioAsync(OpencodeClient client)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var sessionId = Environment.GetEnvironmentVariable("OPENCODE_SESSION_ID");

        await foreach (var item in client.Event.ListAsync(cancellationTokenSource.Token))
        {
            switch (item)
            {
                case SessionIdleEvent idleEvent:
                    Console.WriteLine($"Session idle: {idleEvent.Properties.SessionId}");
                    break;
                case MessageUpdatedEvent messageUpdatedEvent:
                    Console.WriteLine($"Message updated: {messageUpdatedEvent.Properties.Info.Id}");
                    break;
                case SessionErrorEvent sessionErrorEvent:
                    Console.WriteLine($"Session error: {sessionErrorEvent.Properties.Error?.ErrorName ?? "unknown"}");
                    break;
                default:
                    Console.WriteLine($"Event: {item.EventType}");
                    break;
            }

            if (!string.IsNullOrWhiteSpace(sessionId)
                && item is SessionIdleEvent idle
                && string.Equals(idle.Properties.SessionId, sessionId, StringComparison.Ordinal))
            {
                Console.WriteLine("Observed the requested session ID. Stopping enumeration.");
                break;
            }
        }
    }

    private static async Task RunResilienceScenarioAsync()
    {
        var client = CreateClient(TimeSpan.FromSeconds(5));
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            await client.Event.ListAsync(cancellationTokenSource.Token).FirstAsync(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Enumeration canceled by the caller.");
        }
        catch (RequestTimeoutException exception)
        {
            Console.WriteLine($"Request timed out: {exception.Message}");
        }
        catch (ConnectionException exception)
        {
            Console.WriteLine($"Connection failed: {exception.Message}");
        }
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"Set the {name} environment variable before running this scenario.");
    }

    private static SessionChatRequest CreateChatRequest(
        string modelId,
        string providerId,
        string prompt,
        IReadOnlyDictionary<string, bool>? tools = null)
    {
        return new SessionChatRequest
        {
            ModelId = modelId,
            ProviderId = providerId,
            Tools = tools is { Count: > 0 } ? tools : null,
            Parts =
            [
                new TextPartInput
                {
                    Text = prompt,
                },
            ],
        };
    }

    private static async Task<IReadOnlyList<string>> WaitForAssistantReplyTextsAsync(OpencodeClient client, string sessionId)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var assistantTexts = await GetAssistantReplyTextsAsync(client, sessionId);

            if (assistantTexts.Count > 0)
            {
                return assistantTexts;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return await GetAssistantReplyTextsAsync(client, sessionId);
    }

    private static async Task<IReadOnlyList<string>> GetAssistantReplyTextsAsync(OpencodeClient client, string sessionId)
    {
        var results = new List<string>();
        var messages = await client.Session.MessagesAsync(sessionId) ?? [];

        foreach (var entry in messages)
        {
            if (entry.Info is not AssistantMessage)
            {
                continue;
            }

            foreach (var part in entry.Parts)
            {
                if (part is not TextPart textPart)
                {
                    continue;
                }

                var value = textPart.Text;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    results.Add(value);
                }
            }
        }

        return results;
    }

    private static string ResolveChatPrompt(string[] args)
    {
        if (args.Length > 0)
        {
            return string.Join(' ', args).Trim();
        }

        return Environment.GetEnvironmentVariable("OPENCODE_CHAT_PROMPT")
            ?? "Say hello in one short sentence.";
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: dotnet run --project samples/Opencode.Sdk.Samples -- [chat <prompt...>|config|session|events|resilience]");
    }
}

internal static class AsyncEnumerableExtensions
{
    public static async Task<T> FirstAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        await using var enumerator = source.GetAsyncEnumerator(cancellationToken);

        if (await enumerator.MoveNextAsync())
        {
            return enumerator.Current;
        }

        throw new InvalidOperationException("The sequence completed without yielding any items.");
    }
}