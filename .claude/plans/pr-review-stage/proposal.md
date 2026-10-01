---
title: Add pull request and code review stage to the SDLC workflow
status: reviewed
type: change
issue: 3
pr: 4
affects: none
supersedes: none
version: 8.0
created: 2026-10-01T01:04:51+07:00
updated: 2026-10-01T09:18:23+07:00
---

> Retro-filed: the change was made directly in conversation before this proposal existed, then
> filed so it goes through `/review` and `/test` like any other change.

## Problem

The workflow went `/code` → `/test` → local `--no-ff` merge into `main`. No pull request was
opened and nobody reviewed the diff, so code could reach `main` unreviewed and GitHub had no
record linking the change, its review, and its issue.

## Goal

Every change reaches `main` through a pull request that has been reviewed: `/code` opens the PR,
a new `/review` stage (reviewer subagent) reviews it and posts on GitHub, and `/test` merges
through the PR only when the proposal is `reviewed`.

## Target users

Sam (developer workflow). No learner-facing change.

## Success criteria

- `/code` opens `feature/<slug>` → `main` PR with `Closes #<issue>` and records `pr:`.
- `/review` writes `review.md` (blocker/major/nit with file:line), posts it on the PR, sets
  `reviewed` / `changes-requested`.
- `/test` refuses anything not `reviewed` and merges with `gh pr merge --merge`.
- Fix rounds go `/code` → `/review` again; `/status` shows the new statuses and a PR column.
- `gh pr merge/review/comment/edit` are ask-gated; docs and root CLAUDE.md describe the stage.

## Constraints

- GitHub doesn't let an account approve its own PR — the review is posted as a comment and the
  verdict lives in `review.md`.
- No application code changes.
