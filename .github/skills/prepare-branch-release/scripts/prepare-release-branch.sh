#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../../../.." && pwd)"

cd "$repo_root"

usage() {
  cat <<'EOF'
Usage:
  prepare-release-branch.sh [options]

Options:
  --date <yyyy-mm-dd>         Changelog date. Defaults to today.
  --remote <name>             Remote name. Defaults to origin.
  --base-branch <name>        Base branch. Defaults to main.
  --branch <name>             Branch to push. Defaults to current branch.
  --check-only                Print the inferred version, changelog, and commit message without editing the branch.
  --help                      Show this message.
EOF
}

require_clean_branch_name() {
  if [[ -z "$current_branch" || "$current_branch" == "HEAD" ]]; then
    echo "A checked-out branch is required; detached HEAD is not supported." >&2
    exit 1
  fi

  if [[ "$current_branch" == "$base_branch" ]]; then
    echo "Refusing to prepare release directly on '$base_branch'. Use a topic branch." >&2
    exit 1
  fi
}

read_version_from_file() {
  local file_path="$1"

  perl -ne 'if (/<Version>([^<]+)<\/Version>/) { print "$1\n"; $found = 1; last } END { exit($found ? 0 : 1) }' "$file_path"
}

read_version_from_git() {
  local revision="$1"

  git show "$revision:Directory.Build.props" | perl -ne 'if (/<Version>([^<]+)<\/Version>/) { print "$1\n"; $found = 1; last } END { exit($found ? 0 : 1) }'
}

increment_patch_version() {
  local current="$1"

  if [[ ! "$current" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)([-+][0-9A-Za-z.-]+)?$ ]]; then
    echo "Current version '$current' is not a supported semantic version." >&2
    exit 1
  fi

  echo "${BASH_REMATCH[1]}.${BASH_REMATCH[2]}.$((BASH_REMATCH[3] + 1))"
}

humanize_branch_topic() {
  printf '%s\n' "$current_branch" | perl -pe 's{.*/}{}; s/^(feat|feature|fix|bugfix|hotfix|chore|docs|doc|refactor|release|rel|task|test)[-_]+//i; s/[-_]+/ /g; s/\s+/ /g; s/^\s+|\s+$//g; $_ = lc $_; s/\b([a-z])/\U$1/g'
}

add_unique_line() {
  local target_file="$1"
  local value="$2"

  if [[ -z "$value" ]]; then
    return
  fi

  if [[ ! -f "$target_file" ]] || ! grep -Fxq "$value" "$target_file"; then
    printf '%s\n' "$value" >> "$target_file"
  fi
}

collect_change_paths() {
  local output_file="$1"

  : > "$output_file"

  {
    git diff --name-only "$merge_base"..HEAD
    git diff --name-only
    git diff --cached --name-only
    git ls-files --others --exclude-standard
  } | sed '/^$/d' | sort -u | while IFS= read -r path; do
    case "$path" in
      CHANGELOG.md|Directory.Build.props|.github/skills/prepare-branch-release/*)
        ;;
      *)
        printf '%s\n' "$path"
        ;;
    esac
  done > "$output_file"
}

collect_commit_subjects() {
  local output_file="$1"

  git log --format=%s "$merge_base"..HEAD | sed '/^$/d' > "$output_file"
}

has_matching_path() {
  local file_path="$1"
  local pattern="$2"

  grep -Eq "$pattern" "$file_path"
}

append_note() {
  local section="$1"
  local note="$2"

  case "$section" in
    Added)
      add_unique_line "$added_file" "$note"
      ;;
    Changed)
      add_unique_line "$changed_file" "$note"
      ;;
    Fixed)
      add_unique_line "$fixed_file" "$note"
      ;;
    Removed)
      add_unique_line "$removed_file" "$note"
      ;;
    Deprecated)
      add_unique_line "$deprecated_file" "$note"
      ;;
    Security)
      add_unique_line "$security_file" "$note"
      ;;
  esac
}

normalize_commit_subject() {
  printf '%s\n' "$1" | perl -pe 's/^[a-z]+(?:\([^)]+\))?!?:\s*//i; s/\s+/ /g; s/^\s+|\s+$//g; s/\.$//'
}

infer_section_from_subject() {
  local subject_lower

  subject_lower="$(printf '%s\n' "$1" | tr '[:upper:]' '[:lower:]')"

  if [[ "$subject_lower" =~ ^(feat|feature|add)(:|\() ]] || [[ "$subject_lower" =~ (^|[^[:alpha:]])add(s|ed)?([^[:alpha:]]|$) ]]; then
    printf 'Added\n'
  elif [[ "$subject_lower" =~ ^(fix|bugfix|hotfix)(:|\() ]] || [[ "$subject_lower" =~ (^|[^[:alpha:]])fix(es|ed)?([^[:alpha:]]|$) ]]; then
    printf 'Fixed\n'
  elif [[ "$subject_lower" =~ ^security(:|\() ]] || [[ "$subject_lower" =~ (^|[^[:alpha:]])security([^[:alpha:]]|$) ]]; then
    printf 'Security\n'
  elif [[ "$subject_lower" =~ ^(deprecate|deprecated)(:|\() ]] || [[ "$subject_lower" =~ deprecat ]]; then
    printf 'Deprecated\n'
  elif [[ "$subject_lower" =~ ^(remove|removed)(:|\() ]] || [[ "$subject_lower" =~ (^|[^[:alpha:]])remov(e|ed)?([^[:alpha:]]|$) ]]; then
    printf 'Removed\n'
  else
    printf 'Changed\n'
  fi
}

infer_primary_section() {
  local branch_lower

  branch_lower="$(printf '%s\n' "$current_branch" | tr '[:upper:]' '[:lower:]')"

  if [[ "$branch_lower" =~ (^|[-_/])(fix|bugfix|hotfix)([-_/]|$) ]]; then
    printf 'Fixed\n'
    return
  fi

  if [[ "$branch_lower" =~ (^|[-_/])(feat|feature|add)([-_/]|$) ]]; then
    printf 'Added\n'
    return
  fi

  if git diff --diff-filter=A --name-only "$merge_base"..HEAD | grep -q . || git diff --diff-filter=A --name-only | grep -q . || git diff --cached --diff-filter=A --name-only | grep -q . || git ls-files --others --exclude-standard | grep -q .; then
    printf 'Added\n'
    return
  fi

  printf 'Changed\n'
}

append_area_labels() {
  local file_path="$1"

  if has_matching_path "$file_path" '^src/Opencode\.Sdk/(Models/Events|Resources/Events)|^tests/Opencode\.Sdk\.Tests/Event|^docs/events\.md$'; then
    add_unique_line "$areas_file" 'event streaming'
  fi

  if has_matching_path "$file_path" '^src/Opencode\.Sdk/Resources/Session|^tests/Opencode\.Sdk\.Tests/Session'; then
    add_unique_line "$areas_file" 'session APIs'
  fi

  if has_matching_path "$file_path" '^src/Opencode\.Sdk/Internal/Http'; then
    add_unique_line "$areas_file" 'HTTP pipeline behavior'
  fi

  if has_matching_path "$file_path" '^eng/PublicAPI/'; then
    add_unique_line "$areas_file" 'public API surface'
  fi

  if has_matching_path "$file_path" '^samples/'; then
    add_unique_line "$areas_file" 'samples'
  fi

  if has_matching_path "$file_path" '^docs/|^README\.md$'; then
    add_unique_line "$areas_file" 'documentation'
  fi

  if has_matching_path "$file_path" '^tests/'; then
    add_unique_line "$areas_file" 'test coverage'
  fi
}

join_lines() {
  local file_path="$1"
  local limit="$2"
  local result=""
  local count=0
  local line

  while IFS= read -r line; do
    [[ -z "$line" ]] && continue
    count=$((count + 1))
    if [[ "$count" -gt "$limit" ]]; then
      break
    fi

    if [[ -z "$result" ]]; then
      result="$line"
    elif [[ "$count" -eq "$limit" ]]; then
      result="$result, and $line"
    else
      result="$result, $line"
    fi
  done < "$file_path"

  printf '%s\n' "$result"
}

build_primary_note() {
  local section="$1"
  local topic="$2"
  local area_summary="$3"

  case "$section" in
    Added)
      if [[ -n "$topic" && -n "$area_summary" ]]; then
        printf 'Add %s across %s.' "$topic" "$area_summary"
      elif [[ -n "$topic" ]]; then
        printf 'Add %s.' "$topic"
      else
        printf 'Add updates across %s.' "$area_summary"
      fi
      ;;
    Fixed)
      if [[ -n "$topic" && -n "$area_summary" ]]; then
        printf 'Fix %s across %s.' "$topic" "$area_summary"
      elif [[ -n "$topic" ]]; then
        printf 'Fix %s.' "$topic"
      else
        printf 'Fix behavior across %s.' "$area_summary"
      fi
      ;;
    Changed)
      if [[ -n "$topic" && -n "$area_summary" ]]; then
        printf 'Update %s across %s.' "$topic" "$area_summary"
      elif [[ -n "$topic" ]]; then
        printf 'Update %s.' "$topic"
      else
        printf 'Update %s.' "$area_summary"
      fi
      ;;
    *)
      printf 'Update %s.' "$area_summary"
      ;;
  esac
}

infer_notes_from_subjects() {
  local subject
  local normalized
  local section

  while IFS= read -r subject; do
    [[ -z "$subject" ]] && continue
    normalized="$(normalize_commit_subject "$subject")"
    [[ -z "$normalized" ]] && continue
    section="$(infer_section_from_subject "$subject")"
    append_note "$section" "$normalized."
  done < "$subjects_file"
}

infer_notes_from_paths() {
  local primary_section="$1"
  local branch_topic="$2"
  local area_summary="$3"

  if [[ -s "$paths_file" ]]; then
    append_note "$primary_section" "$(build_primary_note "$primary_section" "$branch_topic" "$area_summary")"
  fi

  if has_matching_path "$paths_file" '^docs/|^README\.md$|^samples/'; then
    append_note "Changed" "Refresh documentation and samples to match $area_summary."
  fi

  if has_matching_path "$paths_file" '^tests/'; then
    append_note "Changed" "Expand automated coverage for $area_summary."
  fi

  if has_matching_path "$paths_file" '^eng/PublicAPI/'; then
    append_note "Changed" "Refresh the shipped public API baseline for $area_summary."
  fi
}

render_section_from_file() {
  local title="$1"
  local file_path="$2"
  local line

  if [[ ! -s "$file_path" ]]; then
    return
  fi

  changelog_entry+="### $title"
  changelog_entry+=$'\n\n'

  while IFS= read -r line; do
    [[ -z "$line" ]] && continue
    changelog_entry+="- $line"
    changelog_entry+=$'\n'
  done < "$file_path"

  changelog_entry+=$'\n'
}

insert_changelog_entry() {
  local target_file="$1"
  local temp_file
  local entry_file
  local inserted="false"
  local line

  temp_file="$(mktemp)"
  entry_file="$(mktemp)"

  printf '%s' "$changelog_entry" > "$entry_file"

  while IFS= read -r line || [[ -n "$line" ]]; do
    if [[ "$inserted" == "false" && "$line" == '## ['* ]]; then
      cat "$entry_file" >> "$temp_file"
      inserted="true"
    fi

    printf '%s\n' "$line" >> "$temp_file"
  done < "$target_file"

  if [[ "$inserted" == "false" ]]; then
    if [[ -s "$target_file" ]]; then
      printf '\n' >> "$temp_file"
    fi

      cat "$entry_file" >> "$temp_file"
  fi

  mv "$temp_file" "$target_file"
  rm -f "$entry_file"
}

build_commit_message() {
  local version="$1"
  local topic="$2"
  local area_summary="$3"

  if [[ -n "$topic" ]]; then
    printf 'chore: prepare release %s for %s\n' "$version" "$(printf '%s\n' "$topic" | tr '[:upper:]' '[:lower:]')"
  elif [[ -n "$area_summary" ]]; then
    printf 'chore: prepare release %s for %s\n' "$version" "$area_summary"
  else
    printf 'chore: prepare release %s\n' "$version"
  fi
}

print_inference_summary() {
  printf 'Inferred version: %s\n' "$version"
  printf 'Inferred commit message: %s\n' "$commit_message"
  printf '\n%s' "$changelog_entry"
}

release_date="$(date +%F)"
remote_name="origin"
base_branch="main"
current_branch="$(git branch --show-current)"
check_only="false"

temp_dir="$(mktemp -d)"
trap 'rm -rf "$temp_dir"' EXIT

paths_file="$temp_dir/paths.txt"
subjects_file="$temp_dir/subjects.txt"
areas_file="$temp_dir/areas.txt"
added_file="$temp_dir/added.txt"
changed_file="$temp_dir/changed.txt"
fixed_file="$temp_dir/fixed.txt"
removed_file="$temp_dir/removed.txt"
deprecated_file="$temp_dir/deprecated.txt"
security_file="$temp_dir/security.txt"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --date)
      release_date="$2"
      shift 2
      ;;
    --remote)
      remote_name="$2"
      shift 2
      ;;
    --base-branch)
      base_branch="$2"
      shift 2
      ;;
    --branch)
      current_branch="$2"
      shift 2
      ;;
    --check-only)
      check_only="true"
      shift
      ;;
    --help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

require_clean_branch_name

git fetch "$remote_name" "$base_branch"

base_ref="refs/remotes/$remote_name/$base_branch"
merge_base="$(git merge-base HEAD "$base_ref")"

base_version="$(read_version_from_git "$merge_base")"
current_version="$(read_version_from_file "Directory.Build.props")"
version="$(increment_patch_version "$current_version")"

if [[ "$current_version" != "$base_version" ]]; then
  echo "This branch already changed Directory.Build.props version from $base_version to $current_version." >&2
  echo "Refusing to prepare release twice on the same branch." >&2
  exit 1
fi

if grep -Fq "## [$version] - " CHANGELOG.md; then
  echo "CHANGELOG.md already contains an entry for version $version." >&2
  echo "Refusing to prepare release twice on the same branch." >&2
  exit 1
fi

echo "Duplicate-release guard passed for branch '$current_branch' against '$base_ref'."

collect_change_paths "$paths_file"

if [[ ! -s "$paths_file" ]]; then
  echo "No pending branch changes were found to prepare for release." >&2
  exit 1
fi

collect_commit_subjects "$subjects_file"

: > "$areas_file"
append_area_labels "$paths_file"

branch_topic="$(humanize_branch_topic)"
area_summary="$(join_lines "$areas_file" 3)"

if [[ -z "$area_summary" ]]; then
  area_summary='repository changes'
fi

infer_notes_from_subjects

if [[ ! -s "$added_file" && ! -s "$changed_file" && ! -s "$fixed_file" && ! -s "$removed_file" && ! -s "$deprecated_file" && ! -s "$security_file" ]]; then
  primary_section="$(infer_primary_section)"
  infer_notes_from_paths "$primary_section" "$branch_topic" "$area_summary"
fi

commit_message="$(build_commit_message "$version" "$branch_topic" "$area_summary")"

printf -v changelog_entry '## [%s] - %s\n\n' "$version" "$release_date"
render_section_from_file "Added" "$added_file"
render_section_from_file "Changed" "$changed_file"
render_section_from_file "Fixed" "$fixed_file"
render_section_from_file "Removed" "$removed_file"
render_section_from_file "Deprecated" "$deprecated_file"
render_section_from_file "Security" "$security_file"

if [[ "$check_only" == "true" ]]; then
  print_inference_summary
  exit 0
fi

perl -0pi -e 's|<Version>[^<]+</Version>|<Version>'"$version"'</Version>|' Directory.Build.props

insert_changelog_entry "CHANGELOG.md"

bash eng/verify-release.sh Release

git add -A
git commit -m "$commit_message"
git push --set-upstream "$remote_name" "$current_branch"

echo "Release preparation completed for branch '$current_branch' with version $version."