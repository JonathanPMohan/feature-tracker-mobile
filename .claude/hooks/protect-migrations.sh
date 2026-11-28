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
# What it doesn't cover: shell commands (`sed -i` on a migration). Those are
# never auto-approved by .claude/settings.json, so they'd reach you as a
# permission prompt. Commands YOU run, like `dotnet ef migrations add` and
# `remove`, aren't affected at all: hooks only watch Claude's tools.

set -euo pipefail

# The file Claude wants to change. `jq -r` reads one field from the JSON;
# `// empty` gives an empty string if the field is missing.
file_path="$(jq -r '.tool_input.file_path // empty')"

# Only migration files matter. Everything else: allowed, silently.
if [[ "$file_path" != */Data/Migrations/* ]]; then
  exit 0
fi

# A NEW file in Data/Migrations is allowed: creating isn't changing history.
# (In practice, `dotnet ef migrations add` creates them, not Claude.)
if [[ ! -e "$file_path" ]]; then
  exit 0
fi

# An existing migration file, including the model snapshot: blocked.
cat >&2 <<EOF
Blocked by .claude/hooks/protect-migrations.sh: $(basename "$file_path") is an existing migration file.
Migrations that have run must not change. To fix a migration, add a new one:
  dotnet ef migrations add <Name> --project api/FeatureTracker.Api --output-dir Data/Migrations
If it was never applied anywhere, Jonathan can remove it and add it again:
  dotnet ef migrations remove --project api/FeatureTracker.Api
EOF
exit 2
