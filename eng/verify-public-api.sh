#!/usr/bin/env bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
configuration="${1:-${CONFIGURATION:-Release}}"
configuration_dir="$(printf '%s' "$configuration" | tr '[:upper:]' '[:lower:]')"
assembly_path="$repo_root/artifacts/bin/Opencode.Sdk/$configuration_dir/Opencode.Sdk.dll"
baseline_path="$repo_root/eng/PublicAPI/Opencode.Sdk.PublicAPI.Shipped.txt"
verification_dir="$repo_root/artifacts/verification/public-api"
generated_path="$verification_dir/Opencode.Sdk.PublicAPI.generated.txt"

mkdir -p "$verification_dir"

if [[ ! -f "$assembly_path" ]]; then
    echo "Public API verification failed: expected assembly at $assembly_path" >&2
    exit 1
fi

if [[ ! -f "$baseline_path" ]]; then
    echo "Public API verification failed: missing baseline at $baseline_path" >&2
    exit 1
fi

dotnet run --project "$repo_root/eng/Opencode.ReleaseVerifier/Opencode.ReleaseVerifier.csproj" -c "$configuration" -- public-api "$assembly_path" "$generated_path"

if ! diff -u "$baseline_path" "$generated_path"; then
    echo "Public API verification failed: the current public surface differs from the checked-in baseline." >&2
    exit 1
fi

echo "Public API verification passed for $assembly_path"