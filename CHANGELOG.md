# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog and the project follows Semantic Versioning.

## [1.0.1] - 2026-05-10

### Changed

- Refresh documentation and samples to match event streaming, session APIs, and HTTP pipeline behavior.
- Expand automated coverage for event streaming, session APIs, and HTTP pipeline behavior.
- Refresh the shipped public API baseline for event streaming, session APIs, and HTTP pipeline behavior.

### Fixed

- Fix Sse Deltas across event streaming, session APIs, and HTTP pipeline behavior.

## [1.0.0] - 2026-05-07

### Added

- First public release of the unofficial Opencode .NET SDK.
- `OpencodeClient` entry point with App, Config, Event, File, Find, Session, and Tui resource groups.
- Shared HTTP pipeline with retries, timeouts, diagnostics hooks, typed API exceptions, and Source Link-enabled packaging.
- Session-domain polymorphic models and event streaming via `IAsyncEnumerable<T>`.
- Release verification scripts for package contents, public API baseline, and Source Link validation.
