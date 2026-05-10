---
name: prepare-branch-release
description: 'Prepare a release-ready branch in this repository automatically: detect whether the branch was already release-prepared, bump Directory.Build.props to the next version, infer CHANGELOG.md notes and the commit message from the branch changes, run eng/verify-release.sh Release, then commit and push the branch.'
user-invocable: true
---

# Prepare Branch Release

Use this skill when the user asks to prepare a branch for publication, bump the package version, update the changelog, or leave the branch ready to merge and publish.

This repository publishes packages only when a pull request merged into `main` changes `Directory.Build.props`, so this skill must guard against bumping the version twice on the same branch.

## Procedure

1. Confirm the branch is the feature or release branch to push, not `main`.
2. Run the helper script with no required parameters.

```bash
bash ./.github/skills/prepare-branch-release/scripts/prepare-release-branch.sh
```

3. The script will:
  - fetch `origin/main`
  - compare the current branch against the merge-base with `origin/main`
  - stop if the branch already changed `Directory.Build.props` version or already contains the inferred next changelog heading
  - infer the next patch version from `Directory.Build.props`
  - infer changelog notes from the branch topic, committed delta, and current working tree changes
  - infer the release-preparation commit message from the same change summary
  - update `Directory.Build.props`
  - prepend a new section in `CHANGELOG.md`
  - run `bash eng/verify-release.sh Release`
  - stage all changes, create a commit, and push the current branch with upstream tracking

## Notes

- If the helper reports that the branch already contains a version bump, stop and show that result to the user instead of forcing a second release-prep commit.
- If the branch contains unrelated dirty changes, that is expected: the script stages all current tracked and untracked changes before committing so the branch can be pushed as a single unit.
- If the user needs a dry run first, use `--check-only` to print the inferred version, changelog, and commit message without editing the branch.

## Reference

- Helper script: [prepare-release-branch.sh](./scripts/prepare-release-branch.sh)