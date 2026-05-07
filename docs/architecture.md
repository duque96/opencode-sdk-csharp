# Opencode SDK .NET Architecture

This document records the stable design boundaries of the `Opencode.Sdk` package. It is intended for maintainers and advanced consumers who need to understand why the SDK is shaped the way it is, not for line-by-line implementation walkthroughs.

## Goals

- Keep the public API centered on one `OpencodeClient`.
- Keep resource clients focused on endpoint semantics rather than transport concerns.
- Reuse one shared transport pipeline for request construction, retries, timeouts, diagnostics, and serialization.
- Model the upstream JSON unions explicitly with `System.Text.Json` polymorphism.
- Expose streaming in idiomatic .NET form through `IAsyncEnumerable<T>`.

## Public Entry Point

`OpencodeClient` is the single public entry point for the SDK. Consumers configure shared behavior once through `OpencodeClientOptions` and then navigate to resource clients through properties such as `App`, `Config`, `Session`, and `Event`.

This layout gives the package one clear ownership point for transport configuration while keeping the resource API discoverable in IDE completion. Resource clients are created eagerly from the same client instance and therefore share the same base URL, headers, timeout, retry settings, serializer options, and diagnostics handler.

## Resource Clients

The public resource clients represent stable endpoint groupings:

- `AppClient`
- `ConfigClient`
- `SessionClient`
- `EventClient`
- `FileClient`
- `FindClient`
- `TuiClient`

Each resource client is intentionally thin. It validates obvious caller errors, selects the HTTP method and route, and delegates the rest to the shared pipeline. This keeps endpoint logic easy to audit and avoids copy-pasting retry, timeout, diagnostics, and serialization code across resources.

## Shared Transport Pipeline

`HttpPipeline` is the internal transport boundary that all resources rely on. It owns:

- URI and query construction
- header composition
- request message creation
- JSON serialization and deserialization
- retry decisions and retry delays
- timeout enforcement
- diagnostics dispatch
- streaming response setup

This design keeps transport behavior consistent across both normal request/response APIs and the event stream. When transport behavior changes, the owning abstraction is the pipeline rather than the resource layer.

## Serialization Model

The SDK uses `System.Text.Json` for all public models and request payloads. Session and event contracts rely on explicit discriminator-based polymorphism rather than loose dictionaries or manual converters.

Important consequences of that design:

- Public model hierarchies remain visible and strongly typed in IDE tooling.
- The SDK can deserialize session messages, message parts, tool states, and stream events into stable .NET types.
- `AllowOutOfOrderMetadataProperties` is enabled because the upstream contract does not guarantee discriminator metadata appears before other properties.

The main consumer-facing benefit is predictable, typed payload handling without consumers having to inspect raw JSON for common scenarios.

## Errors and Diagnostics

The public error model separates HTTP failures from transport failures:

- `ApiException` and its typed HTTP subclasses represent non-success HTTP responses.
- `ConnectionException` represents exhausted transport failures.
- `RequestTimeoutException` represents SDK-managed timeout exhaustion.
- `OperationCanceledException` remains the standard .NET signal for caller-driven cancellation.

This split matters because timeouts, cancellation, and server-side rejections require different caller decisions.

Diagnostics are opt-in through `OpencodeClientOptions.DiagnosticsHandler`. The SDK emits stable diagnostic events without requiring an external logging framework. That keeps the package lightweight while still allowing host applications to connect SDK activity to their own telemetry or logging pipeline.

## Streaming

`EventClient.ListAsync` exposes the `/event` SSE endpoint as `IAsyncEnumerable<EventStreamItem>`. That choice aligns the public API with standard .NET asynchronous streaming and makes cancellation and early stop behavior predictable to consumers.

Internally, SSE parsing lives below the resource layer. `EventClient` only selects the route and returns the stream. The pipeline and internal streaming components handle response lifetime, chunk parsing, UTF-8 correctness, and event deserialization.

The practical boundary is:

- Public API: typed async stream of `EventStreamItem`
- Internal responsibility: HTTP streaming semantics and SSE framing

## Documentation Contract

The package README, XML documentation, executable samples, and this architecture document should all describe the same public behavior.

For Phase 7 and later maintenance, documentation should follow these rules:

- Prefer examples derived from focused tests rather than invented examples.
- Do not document per-request timeout or retry overrides unless they are added to the public API.
- Treat misleading examples as defects even when runtime behavior is correct.
- Keep public naming synchronized across docs, samples, and XML comments.

## Maintenance Guidance

When adding a new resource or changing behavior, keep these ownership rules intact:

- Put endpoint-specific validation in the resource client.
- Put shared transport rules in the pipeline.
- Put wire-contract shape in typed models.
- Put stream framing and parsing in internal streaming infrastructure.
- Update README snippets and the executable sample whenever a user-facing workflow changes.

Related documents:

- Sample index: https://github.com/duque96/opencode-sdk-csharp/blob/main/samples/README.md
- Coverage guide: https://github.com/duque96/opencode-sdk-csharp/blob/main/docs/openapi-coverage.md