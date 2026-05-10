
using Opencode.Client;
using Opencode.Models.Events;
using Opencode.Models.Session;

var clientOptions = new OpencodeClientOptions
{
    BaseUrl = new Uri("http://localhost:54321"),
    Timeout = TimeSpan.FromMinutes(2),
    MaxRetries = 0,
};

clientOptions.DefaultHeaders["X-Sample-Name"] = "sdk-console-chat-sample";
var client = new OpencodeClient(clientOptions);

var session = (await client.Session.CreateAsync())!;

var eventsTask = Task.Run(async () =>
{
    var messages = new Dictionary<string, SessionMessage>();

    await foreach (var eventItem in client.Event.ListAsync())
    {
        switch (eventItem)
        {
            case MessageUpdatedEvent messageUpdatedEvent
                when messageUpdatedEvent.Properties.Info.SessionId == session.Id:
                messages[messageUpdatedEvent.Properties.Info.Id] = messageUpdatedEvent.Properties.Info;
                break;

            case MessagePartDeltaEvent messagePartUpdatedEvent
                when messagePartUpdatedEvent.Properties.SessionId == session.Id:

                if (!messages.TryGetValue(messagePartUpdatedEvent.Properties.MessageId, out var message) ||
                    message.MessageRole != "assistant")
                {
                    continue;
                }

                Console.Write(messagePartUpdatedEvent.Properties.Delta);
                break;
        }
    }
});

while (true)
{
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        var request = new SessionChatRequest { ModelId = "big-pickle", ProviderId = "opencode", Parts = [new TextPartInput { Text = input }] };
        await client.Session.PromptAsync(session.Id, request);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error sending message: {ex.Message}");
    }
}

await eventsTask;