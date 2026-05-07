# Samples

This folder contains executable examples for the Opencode .NET SDK.

## Available sample

- `Opencode.Sdk.MinimalSample`: minimal connectivity check against a real Opencode server using `GET /app`.
- `Opencode.Sdk.Samples`: console sample with focused scenarios for configuration lookup, small chat flows, session workflows, event streaming, and transport/error handling.

## Fastest real-server check

If you only want to verify that the SDK can talk to a real Opencode server, run the minimal sample:

```bash
dotnet run --project samples/Opencode.Sdk.MinimalSample/Opencode.Sdk.MinimalSample.csproj -- http://localhost:54321
```

You can also omit the URL argument and use `OPENCODE_BASE_URL` instead.

## Run locally

Set these environment variables before running the sample:

- `OPENCODE_BASE_URL`: base URL for the Opencode service. Defaults to `http://localhost:54321`.
- `OPENCODE_CHAT_PROMPT`: optional default prompt for the `chat` scenario.
- `OPENCODE_MODEL_ID`: model identifier used by the session scenarios.
- `OPENCODE_PROVIDER_ID`: provider identifier used by the session scenarios.
- `OPENCODE_SESSION_ID`: optional existing session ID for the `events` scenario.

To run the smallest real chat against a local server:

```bash
OPENCODE_BASE_URL=http://127.0.0.1:54321 \
OPENCODE_PROVIDER_ID=opencode \
OPENCODE_MODEL_ID=big-pickle \
dotnet run --project samples/Opencode.Sdk.Samples/Opencode.Sdk.Samples.csproj -- chat "Hola, responde con una frase corta."
```

Run one scenario at a time:

```bash
dotnet run --project samples/Opencode.Sdk.MinimalSample/Opencode.Sdk.MinimalSample.csproj -- http://localhost:54321
dotnet run --project samples/Opencode.Sdk.Samples/Opencode.Sdk.Samples.csproj -- chat "Hola"
dotnet run --project samples/Opencode.Sdk.Samples/Opencode.Sdk.Samples.csproj -- config
dotnet run --project samples/Opencode.Sdk.Samples/Opencode.Sdk.Samples.csproj -- session
dotnet run --project samples/Opencode.Sdk.Samples/Opencode.Sdk.Samples.csproj -- events
dotnet run --project samples/Opencode.Sdk.Samples/Opencode.Sdk.Samples.csproj -- resilience
```

The sample is intentionally small and mirrors the workflows validated in the session and event test suites.