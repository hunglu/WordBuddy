---
title: Test — verify an implemented plan
description: Stage 5 of the propose → plan → code → review → test → release workflow. Invokes the tester subagent to write/extend unit, integration, and E2E tests and report results.
status: draft
---

## Context

Stage 5 of 6: **propose → plan → code → review → test → release**. Verification is done by the `tester` subagent
(`.claude/agents/tester.md`), which owns unit/integration tests inside each touched service's
existing `UnitTests`/`IntegrationTests` projects, plus the cross-cutting E2E projects
`e2e/api/WordBuddy.E2E.Api.Tests` (.NET Playwright, API-level) and `e2e/ui` (TypeScript
Playwright + playwright-bdd, UI-level).

Argument: `$ARGUMENTS` is the slug. If missing, list folders under `.claude/plans/` with
`status: reviewed` and ask Sam which one.

## Steps

1. **Validate.** Confirm `.claude/plans/<slug>/plan.md` and `tasks.md` exist and `proposal.md`'s
   status is `reviewed` or `needs-fixes`. If it's `implemented` or `changes-requested`, tell Sam
   to run `/review <slug>` (or `/code <slug>` to fix findings) first — code is never merged
   unreviewed. Whether a `needs-fixes` re-run may merge is decided by the **merge guard**
   (`docs/sdlc/workflow.md` → Merge guard), not by status alone: if application code changed
   after the reviewed commit, the tester refuses to merge and Sam runs `/review` again.

   Also confirm the `feature/<slug>` branch exists locally or on `origin`. If it doesn't, tell
   Sam to run `/code <slug>` first.

2. **Invoke the `tester` subagent** (via the Agent tool, `subagent_type: tester`), passing it
   `plan.md` (to know what was built and which service(s)/frontend areas it touched) and the
   slug. It switches to `feature/<slug>`, merges in the latest `main`, and tests that.

3. The tester subagent writes and runs tests, and reports pass/fail. If it needed to run
   anything `ask`-gated (`docker compose up` to have live services for E2E, for example), it
   should have stopped and asked Sam directly rather than working around it — if it reports that
   happened, relay it to Sam plainly.

4. **Write `.claude/plans/<slug>/test-report.md`** summarizing what ran and the outcome (this can
   be done by the tester subagent directly, or by you from its report — either way it must exist
   after this command). If the run taught a reusable lesson, the tester appends it to
   `docs/ai/learnings.md` (format in that file) before its commit — never to a `CLAUDE.md`.

5. **Status, approval, and merge are done by the tester subagent**, in the order set by
   `.claude/agents/tester.md` (steps 1–5), with **one commit + one push per run**: resolve the PR
   (`pr:` may lag — `gh pr list --head` fallback) → verdict → not approved: one commit
   `<slug>: tests, report, needs fixes` + push, board sync **In progress** → approved: one commit
   `<slug>: tests, report, mark done` + push → mergeability check (after the push, since the push
   carries the `origin/main` merge) → merge **through the PR** with `gh pr merge <pr> --merge`
   (local `--no-ff` only when no PR exists) → board sync check **Done**. If the PR isn't
   mergeable or the merge fails/is declined, one corrective `<slug>: undo mark done — <reason>`
   commit (path-limited
   `git restore --source=<done sha>^ --staged --worktree -- .claude/plans/<slug>/proposal.md docs/features/`,
   not a `git revert`) — the one documented exception to one commit per cycle.
   Pushes, the PR merge and board writes are ask-gated, so Sam approves each one. Don't repeat
   any of these steps yourself. Just check that they happened.

   **Conflicts leave the status as it is** (Sam's rule): if `main` can't be merged into the
   branch, the tester changes and commits nothing. If GitHub reports the PR as conflicting or
   still `UNKNOWN` after the push, the "mark done" is undone, so the status ends as it was and
   nothing is merged. That is an expected outcome, not a failed run — don't set
   `needs-fixes` yourself. The PR stays open until the conflict is resolved via `/code`, then
   `/review`.

6. **Report back to Sam**: the verdict (merged into `main` or not, and why). In the failing or
   skipped case, summarize the failures so Sam knows whether to loop back to `/code <slug>`
   (fixes continue on the same `feature/<slug>` branch) or `/plan <slug>`. For a conflict, name
   the conflicting files and say the next step is `/code <slug>` to resolve them, then `/review`.

## Verification

- `.claude/plans/<slug>/test-report.md` exists and states a clear outcome and verdict.
- `proposal.md` status after the run is one of:
  - `done` — and the PR is merged (`gh pr view <pr> --json state` is `MERGED`; with no PR,
    `git branch --merged main` lists `feature/<slug>`);
  - `needs-fixes` — guard or suite failure, nothing merged;
  - unchanged (`reviewed` / `needs-fixes` as it was before) — conflict, Sam declined a push or
    the merge; nothing merged, PR still open.
- `done` is never left on `feature/<slug>` without the merge (if the PR wasn't mergeable or the
  merge failed, the path-limited "undo mark done" restore removed it from `proposal.md` and
  `docs/features/`, including files the "mark done" commit added; tests stay).
- Commits added by the run, by case:
  - local `git merge origin/main` conflicts → none, nothing pushed;
  - not approved → `tests, report, needs fixes` + one push;
  - approved and merged → `tests, report, mark done` + one push;
  - GitHub reports `CONFLICTING`/`UNKNOWN` after the push, or the merge fails/is declined →
    `tests, report, mark done` then `undo mark done — <reason>` (the one documented exception to
    one commit per cycle).
  Plus at most the local `origin/main` merge commit.
- Board Status: Done after a merge, In progress after `needs-fixes`, or the skip reason is
  reported.
- The tester changed only allowlisted paths (`docs/sdlc/workflow.md` → Merge guard): test
  projects, `e2e/`, `docs/features/`, `docs/ai/learnings.md`, and in `.claude/plans/<slug>/` only `test-report.md`,
  `proposal.md`, `review.md` — apart from the merge commit itself.

## Scope suggestions

If the subagent report has a `## Scope suggestions` list, post it as **one issue comment** before reporting back (`docs/sdlc/github-integration.md` → Scope suggestions; `gh issue comment`, ask-gated). Never edit the issue body or title. Include the suggestions in the report to Sam.
