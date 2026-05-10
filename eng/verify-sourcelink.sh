#!/usr/bin/env bash

set -euo pipefail

fail() {
    echo "Source Link verification failed: $1" >&2
    exit 1
}

require_line() {
    local haystack="$1"
    local needle="$2"

    if ! grep -Fq "$needle" <<<"$haystack"; then
        fail "Expected to find '$needle'."
    fi
}

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
configuration="${1:-${CONFIGURATION:-Release}}"
configuration_dir="$(printf '%s' "$configuration" | tr '[:upper:]' '[:lower:]')"
package_dir="$repo_root/artifacts/package/$configuration_dir"
verification_dir="$repo_root/artifacts/verification/sourcelink"
pdb_path="$repo_root/artifacts/bin/Opencode.Sdk/$configuration_dir/Opencode.Sdk.pdb"
package_id="Opencode.Sdk.CSharp"

mkdir -p "$verification_dir"

snupkg_path="$(find "$package_dir" -maxdepth 1 -type f -name "$package_id.*.snupkg" | sort | tail -n 1)"

if [[ -z "$snupkg_path" ]]; then
    fail "No .snupkg was found under $package_dir."
fi

if [[ ! -f "$pdb_path" ]]; then
    fail "Expected PDB at $pdb_path."
fi

symbol_contents="$(unzip -Z1 "$snupkg_path")"
printf '%s\n' "$symbol_contents" > "$verification_dir/Opencode.Sdk.symbol-package.contents.txt"
require_line "$symbol_contents" "lib/net10.0/Opencode.Sdk.pdb"

sourcelink_lines="$(strings "$pdb_path" | grep -E 'raw\.githubusercontent\.com/duque96/opencode-sdk-csharp|github\.com/duque96/opencode-sdk-csharp' || true)"
printf '%s\n' "$sourcelink_lines" > "$verification_dir/Opencode.Sdk.sourcelink.txt"

if [[ -z "$sourcelink_lines" ]]; then
    fail "No Source Link markers were found in the generated PDB."
fi

require_line "$sourcelink_lines" "raw.githubusercontent.com/duque96/opencode-sdk-csharp"

echo "Source Link verification passed for $snupkg_path"