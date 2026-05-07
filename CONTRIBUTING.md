# Contributing

## Development setup

This repository targets the .NET SDK pinned in [global.json](global.json) and uses centralized build settings from [Directory.Build.props](Directory.Build.props) and [Directory.Packages.props](Directory.Packages.props).

Set up the repository with:

```bash
dotnet restore
dotnet build Opencode.Sdk.slnx
```

## Project structure

- [src/Opencode.Sdk](src/Opencode.Sdk): packable SDK library
- [tests/Opencode.Sdk.Tests](tests/Opencode.Sdk.Tests): unit and integration-style tests against controlled handlers
- [samples](samples): runnable sample applications
- [eng](eng): release verification scripts and tooling
- [docs](docs): architecture notes and coverage docs

## Making changes

Keep changes focused and consistent with the existing public API shape.

- Prefer idiomatic C# APIs and keep the public surface coherent for .NET consumers.
- Keep user-facing package behavior aligned with the reduced reference surface documented in [docs/openapi-coverage.md](docs/openapi-coverage.md).
- Add or update XML documentation for public surface changes.
- Add tests for each functional change. Resource behavior should have focused tests in [tests/Opencode.Sdk.Tests](tests/Opencode.Sdk.Tests).
- Avoid unrelated refactors in release-sensitive areas.

## Validation

Before opening a pull request, run:

```bash
bash eng/verify-release.sh Release
```

This gate restores, builds, tests, packs, and verifies the package, Source Link, and public API baseline.

If you change the public API intentionally, update the shipped baseline under [eng/PublicAPI](eng/PublicAPI) using the existing release verifier flow and include that change in your review context.

## Pull requests

Pull requests should include:

- A short description of the behavior change
- Any API or package impact
- The validation command(s) you ran
- Relevant documentation or sample updates when consumer behavior changes

Commit messages should follow the Conventional Commits format so the automated release flow can infer semantic version bumps:

- `fix:` for patch releases
- `feat:` for minor releases
- `feat!:` or any `type!:` for major releases

## Releases

Versioning and changelog updates are managed by release-please via [.github/workflows/release-please.yml](.github/workflows/release-please.yml), [release-please-config.json](release-please-config.json), and [.release-please-manifest.json](.release-please-manifest.json).

NuGet publication reuses the same verification gate in CI and runs automatically from the release-please workflow whenever a GitHub release is created. The workflow requires the `NUGET_API_KEY` repository secret.

The manual fallback workflow in [.github/workflows/publish-nuget.yml](.github/workflows/publish-nuget.yml) can be used to republish an already tagged version if the automated publication step fails.