#!/usr/bin/env bash

set -euo pipefail

fail() {
    echo "Package verification failed: $1" >&2
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
verification_dir="$repo_root/artifacts/verification/package"
package_id="Opencode.Sdk"
package_version="$(grep -m1 '<Version>' "$repo_root/Directory.Build.props" | sed -E 's/.*<Version>([^<]+)<\/Version>.*/\1/')"

mkdir -p "$verification_dir"

nupkg_path="$package_dir/$package_id.$package_version.nupkg"

if [[ -z "$package_version" ]]; then
    fail "Unable to resolve package version from Directory.Build.props."
fi

if [[ ! -f "$nupkg_path" ]]; then
    fail "No .nupkg was found under $package_dir."
fi

package_contents="$(unzip -Z1 "$nupkg_path")"
printf '%s\n' "$package_contents" > "$verification_dir/Opencode.Sdk.package.contents.txt"

require_line "$package_contents" "$package_id.nuspec"
require_line "$package_contents" "lib/net10.0/Opencode.Sdk.dll"
require_line "$package_contents" "lib/net10.0/Opencode.Sdk.xml"
require_line "$package_contents" "README.md"
require_line "$package_contents" "LICENSE"

if grep -Eq 'Opencode\.Sdk\.(Tests|Samples|MinimalSample)\.dll$' <<<"$package_contents"; then
    fail "The package contains a sample or test assembly."
fi

nuspec_contents="$(unzip -p "$nupkg_path" "$package_id.nuspec")"
printf '%s\n' "$nuspec_contents" > "$verification_dir/Opencode.Sdk.nuspec"

require_line "$nuspec_contents" "<id>$package_id</id>"
require_line "$nuspec_contents" "<authors>DDB</authors>"
require_line "$nuspec_contents" "<description>A community-maintained, unofficial C# SDK for the Opencode API</description>"
require_line "$nuspec_contents" "<readme>README.md</readme>"
require_line "$nuspec_contents" "<license type=\"file\">LICENSE</license>"
require_line "$nuspec_contents" "<projectUrl>https://github.com/duque96/opencode-sdk-csharp</projectUrl>"
require_line "$nuspec_contents" "<tags>opencode api sdk csharp dotnet</tags>"
require_line "$nuspec_contents" "<repository type=\"git\" url=\"https://github.com/duque96/opencode-sdk-csharp\""

echo "Package verification passed for $nupkg_path"