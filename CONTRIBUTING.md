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

## Releases

Versioning is managed manually in [Directory.Build.props](Directory.Build.props). Update the `<Version>` value before publishing a new package.

Before publishing, run:

```bash
bash eng/verify-release.sh Release
```

Publication runs automatically through [.github/workflows/publish-github-packages.yml](.github/workflows/publish-github-packages.yml) only when a pull request is merged into `main` and that merge changes the `<Version>` value in [Directory.Build.props](Directory.Build.props). The workflow can also be launched manually from GitHub Actions when needed, for example to retry a failed publish. It publishes the generated package to both GitHub Packages and nuget.org. GitHub Packages uses the repository `GITHUB_TOKEN`, and nuget.org requires the `NUGET_API_KEY` repository secret.

If you keep a release history, update [CHANGELOG.md](CHANGELOG.md) manually as part of the version bump.