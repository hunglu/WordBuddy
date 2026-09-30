# 0002 — Spec-driven, agent-assisted SDLC

- Status: accepted
- Date: 2026-09-30

## Context

Features keep changing; CLAUDE.md drifted because agents wrote run history into it.

## Decision

- Workflow `/propose → /plan → /code → /test → /release`, with `/status` as the board view.
- Each change is a frozen folder in `.claude/plans/<slug>/`; changes to shipped features get a
  *new* proposal (`type: change`, `supersedes:`).
- Current behaviour lives in `docs/features/*.md` (living specs), updated on merge.
- CLAUDE.md files hold stable rules only; editing them is `ask`-gated.

## Consequences

Two places to read (spec vs. history) but neither goes stale.
