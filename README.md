# Unofficial Opencode SDK for .NET

`Opencode.Sdk` is a community-maintained, unofficial .NET SDK for the Opencode HTTP API. It targets `.NET 10`, exposes a single `OpencodeClient` entry point, and keeps both request/response APIs and event streaming in idiomatic C# forms.

This repository is maintained independently by DDB. It does not belong to Opencode and is not an official Opencode project or release.

## Installation

```bash
dotnet add package DDB.Opencode.Sdk
```

If you need to consume the package from GitHub Packages instead of nuget.org, add the GitHub feed and authenticate with a GitHub personal access token with at least `read:packages` scope:

```bash
dotnet nuget add source "https://nuget.pkg.github.com/duque96/index.json" \
	--name github-opencode \
	--username YOUR_GITHUB_USERNAME \
	--password YOUR_GITHUB_PAT \
	--store-password-in-clear-text

dotnet add package DDB.Opencode.Sdk --source github-opencode
```

## Requirements

- .NET SDK `10.0.100` or later in the `.NET 10` feature band
- Access to a running Opencode service. The default base URL is `http://localhost:54321`.

## Running Opencode Server

To start Opencode as a standalone HTTP server, use:

```bash
opencode serve --hostname 127.0.0.1 --port 4096
```

The SDK base URL should then point to `http://127.0.0.1:4096`.

The server publishes its OpenAPI document at:

```text
http://127.0.0.1:4096/doc
```

If you run the interactive `opencode` client instead of `opencode serve`, Opencode also starts an HTTP server. In that local mode this SDK defaults to `http://localhost:54321`, so the OpenAPI document is typically available at `http://localhost:54321/doc`.

See `docs/openapi-coverage.md` for the current SDK coverage against the documented server API.

## Quick Start

```csharp
using Opencode.Client;

var client = new OpencodeClient(new OpencodeClientOptions
{
	BaseUrl = new Uri("http://localhost:54321", UriKind.Absolute),
});

var app = await client.App.GetAsync();
var config = await client.Config.GetAsync();
var sessions = await client.Session.ListAsync();
```

## Resource Overview

`OpencodeClient` exposes the currently implemented resource groups directly:

- `client.App`
- `client.Config`
- `client.Event`
- `client.File`
- `client.Find`
- `client.Session`
- `client.Tui`

Each resource client stays thin and delegates transport, retries, timeout handling, JSON serialization, and diagnostics to the shared pipeline configured through `OpencodeClientOptions`.

The current implementation covers a deliberate subset of the server API documented at `https://opencode.ai/docs/es/server/`.

## Session Workflow

The most complete non-streaming workflow in the current SDK is the session domain.

```csharp
using Opencode.Client;
using Opencode.Models.Session;

var client = new OpencodeClient(new OpencodeClientOptions
{
	BaseUrl = new Uri("http://localhost:54321", UriKind.Absolute),
});

var session = await client.Session.CreateAsync()
	?? throw new InvalidOperationException("The service returned no session payload.");

await client.Session.InitAsync(session.Id, new SessionInitRequest
{
	MessageId = "bootstrap",
	ModelId = "gpt-5.4",
	ProviderId = "openai",
});

var assistantMessage = await client.Session.ChatAsync(session.Id, new SessionChatRequest
{
	ModelId = "gpt-5.4",
	ProviderId = "openai",
	Parts =
	[
		new TextPartInput
		{
			Text = "Summarize the current project goals in one sentence.",
		},
	],
	Tools = new Dictionary<string, bool>
	{
		["grep"] = true,
	},
});

var messages = await client.Session.MessagesAsync(session.Id) ?? [];

Console.WriteLine($"Session {session.Id} now has {messages.Length} messages.");
Console.WriteLine($"Assistant reply ID: {assistantMessage?.Id}");
```

The supported session surface also includes delete, abort, revert, share, summarize, unrevert, and unshare operations.

## Event Streaming

`EventClient.ListAsync` exposes server-sent events as `IAsyncEnumerable<EventStreamItem>` so callers can consume them with `await foreach` and standard cancellation tokens.

The full catalog of currently supported event types lives in [docs/events.md](docs/events.md).

```csharp
using Opencode.Client;
using Opencode.Models.Events;

var client = new OpencodeClient(new OpencodeClientOptions());
using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

await foreach (var item in client.Event.ListAsync(cancellationTokenSource.Token))
{
	switch (item)
	{
		case SessionIdleEvent idleEvent:
			Console.WriteLine($"Session idle: {idleEvent.Properties.SessionId}");
			break;
		case MessageUpdatedEvent updatedEvent:
			Console.WriteLine($"Message updated: {updatedEvent.Properties.Info.Id}");
			break;
		case SessionErrorEvent errorEvent:
			Console.WriteLine($"Session error: {errorEvent.Properties.Error?.ErrorName ?? "unknown"}");
			break;
		default:
			Console.WriteLine(item.EventType);
			break;
	}
}
```

Breaking out of the loop stops enumeration and disposes the underlying streaming response. Canceling the supplied token stops active reads and surfaces `OperationCanceledException`.

## Errors

All SDK-specific failures derive from `OpencodeException`.

- `ApiException`: the service returned a non-success HTTP status code. The exception carries `StatusCode`, `Headers`, `ResponseBody`, and `RequestId` when available.
- `BadRequestException`, `UnauthorizedException`, `ForbiddenException`, `NotFoundException`, `ConflictException`, `UnprocessableEntityException`, `RateLimitException`, and `InternalServerErrorException`: typed HTTP failure variants.
- `ConnectionException`: the request failed at the transport layer after retries were exhausted.
- `RequestTimeoutException`: the SDK-managed timeout elapsed before the request completed.
- `UnexpectedResponseException`: the service returned a success status with a non-JSON or incompatible JSON payload for the SDK contract.

Typical handling looks like this:

```csharp
using Opencode.Errors;

try
{
	var config = await client.Config.GetAsync();
}
catch (RequestTimeoutException exception)
{
	Console.WriteLine($"Timed out after {exception.Timeout}.");
}
catch (ApiException exception)
{
	Console.WriteLine($"HTTP {(int)exception.StatusCode}: {exception.Message}");
	Console.WriteLine(exception.ResponseBody);
}
catch (ConnectionException exception)
{
	Console.WriteLine($"Transport failure: {exception.Message}");
}
```

## Cancellation, Timeouts, and Retries

`OpencodeClientOptions` centralizes the shared transport knobs for all resources.

```csharp
using Opencode.Client;
using Opencode.Diagnostics;

var options = new OpencodeClientOptions
{
	BaseUrl = new Uri("http://localhost:54321", UriKind.Absolute),
	Timeout = TimeSpan.FromSeconds(20),
	MaxRetries = 1,
	DiagnosticsHandler = static diagnosticEvent =>
		Console.WriteLine($"[{diagnosticEvent.Kind}] {diagnosticEvent.Method} {diagnosticEvent.Path} attempt={diagnosticEvent.Attempt}"),
};

var client = new OpencodeClient(options);
using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

var config = await client.Config.GetAsync(cancellationTokenSource.Token);
```

- `Timeout` applies at the client level.
- `MaxRetries` controls retry attempts for transient failures.
- `CancellationToken` remains the per-call mechanism for caller-driven cancellation.
- `DiagnosticsHandler` receives stable pipeline events without forcing a logging abstraction into the public API.

## Samples and Documentation

- Sample index: https://github.com/duque96/opencode-sdk-csharp/blob/main/samples/README.md
- Executable sample project: https://github.com/duque96/opencode-sdk-csharp/tree/main/samples/Opencode.Sdk.Samples
- Architecture guide: https://github.com/duque96/opencode-sdk-csharp/blob/main/docs/architecture.md
- Coverage guide: https://github.com/duque96/opencode-sdk-csharp/blob/main/docs/openapi-coverage.md

## Local Development

```bash
dotnet restore Opencode.Sdk.slnx
dotnet build Opencode.Sdk.slnx
dotnet test tests/Opencode.Sdk.Tests/Opencode.Sdk.Tests.csproj --filter SessionClientTests
dotnet test tests/Opencode.Sdk.Tests/Opencode.Sdk.Tests.csproj --filter EventClientTests
bash eng/verify-release.sh
dotnet run --project samples/Opencode.Sdk.Samples/Opencode.Sdk.Samples.csproj -- session
```
