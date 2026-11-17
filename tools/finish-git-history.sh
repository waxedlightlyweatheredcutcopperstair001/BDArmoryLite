#!/bin/bash
# One-off: turn the version snapshots (saved before git had a user name/email) into real commits.
# Run after:  git config --global user.name "Your Name"  and  git config --global user.email "you@example.com"
set -e
cd "$(dirname "$0")/.."
git config user.name >/dev/null || { echo "Set git user.name / user.email first."; exit 1; }
trailer=$'\n\nCo-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>'
parent=""
for v in 1.1.0 1.2.0; do
  case $v in
    1.1.0) msg="BDArmory Lite 1.1.0: trimmed BDArmory 1.6.11 + NKDR, arming/detonation rework, dark UI, VEFX support";;
    1.2.0) msg="BDArmory Lite 1.2.0: VAB Organizer subcategories, remove sub-tab bar, back to BDArmoryLite folder";;
  esac
  c=$(git commit-tree "v$v-snapshot" ${parent:+-p $parent} -m "$msg$trailer")
  git tag "v$v" "$c"
  git tag -d "v$v-snapshot" >/dev/null
  parent=$c
done
git update-ref refs/heads/main "$parent"
git reset -q   # Sync the index with the new commit.
git log --oneline --decorate
