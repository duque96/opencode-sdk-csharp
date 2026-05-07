#!/usr/bin/env bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
configuration="${1:-${CONFIGURATION:-Release}}"

cd "$repo_root"

dotnet restore Opencode.Sdk.slnx
dotnet build Opencode.Sdk.slnx -c "$configuration" --no-restore
dotnet test Opencode.Sdk.slnx -c "$configuration" --no-build
dotnet pack src/Opencode.Sdk/Opencode.Sdk.csproj -c "$configuration" --no-build

bash "$repo_root/eng/verify-package.sh" "$configuration"
bash "$repo_root/eng/verify-sourcelink.sh" "$configuration"
bash "$repo_root/eng/verify-public-api.sh" "$configuration"