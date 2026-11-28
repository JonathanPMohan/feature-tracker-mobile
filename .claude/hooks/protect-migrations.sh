#!/usr/bin/env bash
#
# Claude Code hook (M2, decision D): Claude may never change an existing
# migration.
#
# A migration that has run is history: it's already in a database, so
# editing it makes the code and the database disagree, silently. The fix for
# a wrong migration is always a NEW migration (or, if it was never applied
# anywhere, `dotnet ef migrations remove` and add it again).
#
# How it runs: .claude/settings.json registers this as a PreToolUse hook for
# Claude's file-editing tools. Before Claude edits or writes a file, Claude
# Code runs this script and sends it the planned tool call as JSON on stdin:
#   {"tool_name": "Edit", "tool_input": {"file_path": "/…/Data/Migrations/…"}, …}
# Exit code 0 lets the edit happen. Exit code 2 blocks it, and whatever this
# script prints to stderr is shown to Claude as the reason.
#
# FAIL CLOSED: any OTHER exit code (a crash, a missing tool) counts as a
# non-blocking error, and the edit goes ahead. A safety check that breaks
# quietly is worse than none, so every way this script can fail ends in an
# explicit `exit 2` below. If it's broken, Claude's edits stop, loudly, until
# it's fixed. (Found by /code-review on PR #4.)
#
# What it doesn't cover: shell commands (`sed -i` on a migration). Those are
# never auto-approved by .claude/settings.json, so they'd reach you as a
# permission prompt. Commands YOU run, like `dotnet ef migrations add` and
# `remove`, aren't affected at all: hooks only watch Claude's tools.

# No `set -e` on purpose: a failing command must reach the explicit
# `exit 2` handling below instead of ending the script with its own code.
set -uo pipefail

block() {
  printf 'Blocked by .claude/hooks/protect-migrations.sh: %s\n' "$1" >&2
  exit 2
}

# jq reads JSON. macOS 15 and later ship it in /usr/bin; older systems need
# `brew install jq`. Without it, we can't tell which file is being edited.
command -v jq >/dev/null 2>&1 ||
  block "the hook needs jq to read Claude's tool call, and jq isn't installed. Install it (brew install jq), or ask Jonathan."

# The file Claude wants to change. `jq -er` exits non-zero if the JSON is
# unreadable or the field is missing.
file_path="$(jq -er '.tool_input.file_path' 2>/dev/null)" ||
  block "couldn't read tool_input.file_path from the hook's input, so the edit can't be checked."

# Compare in lowercase: macOS's filesystem ignores case, so
# `.../data/migrations/X.cs` opens the same file as `.../Data/Migrations/X.cs`.
# (`tr`, not bash's ${var,,}: macOS's built-in bash is version 3.2, which
# doesn't have it.)
lower_path="$(printf '%s' "$file_path" | tr '[:upper:]' '[:lower:]')"

# Only migration files matter. Everything else: allowed, silently.
if [[ "$lower_path" != */data/migrations/* ]]; then
  exit 0
fi

# A NEW file in Data/Migrations is allowed: creating isn't changing history.
# (In practice, `dotnet ef migrations add` creates them, not Claude.)
# `-e` also ignores case on macOS, so it finds the file however it's spelled.
if [[ ! -e "$file_path" ]]; then
  exit 0
fi

# An existing migration file, including the model snapshot: blocked.
block "$(basename "$file_path") is an existing migration file.
Migrations that have run must not change. To fix a migration, add a new one:
  dotnet ef migrations add <Name> --project api/FeatureTracker.Api --output-dir Data/Migrations
If it was never applied anywhere, Jonathan can remove it and add it again:
  dotnet ef migrations remove --project api/FeatureTracker.Api"
