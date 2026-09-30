# 0003 — GitHub Issues + Projects as the idea intake and Kanban

- Status: accepted
- Date: 2026-09-30

## Context
Need a visible backlog at zero cost. Jira/Trello add a second system and paid tiers for automation.

## Decision
GitHub Issues (intake) + GitHub Projects v2 (Kanban) with built-in workflows. Claude talks to
GitHub through the `gh` CLI locally, under the existing Claude subscription. No Claude-in-Actions
(it bills API usage). See `../sdlc/github-integration.md`.

## Consequences
Issue ↔ plan linking is by convention (`issue:` frontmatter, `#n` in commit/PR text).
