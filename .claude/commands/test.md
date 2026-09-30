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
   status is `reviewed`. If it's `implemented` or `changes-requested`, tell Sam to run
   `/review <slug>` (or `/code <slug>` to fix findings) first — code is never merged unreviewed.
   A `needs-fixes` branch goes back through `/code` → `/review` before `/test` again. Exception:
   Sam explicitly says to re-test without code changes (e.g. services were down) — then
   `needs-fixes` is accepted.

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
   after this command).

5. **Status, approval, and merge are done by the tester subagent.** It sets `proposal.md` to
   `status: done` or `status: needs-fixes`, commits and pushes `feature/<slug>`, and, only when
   every suite ran and passed, merges `feature/<slug>` into `main` with `--no-ff` and pushes
   `main`. Both pushes are ask-gated, so Sam approves each one. Don't repeat any of these steps
   yourself. Just check that they happened.

6. **Report back to Sam**: the verdict (merged into `main` or not, and why). In the failing or
   skipped case, summarize the failures so Sam knows whether to loop back to `/code <slug>`
   (fixes continue on the same `feature/<slug>` branch) or `/plan <slug>`.

## Verification

- `.claude/plans/<slug>/test-report.md` exists and states a clear pass/fail outcome and verdict.
- `proposal.md` status is `done` or `needs-fixes`, never left at `implemented`.
- `status: done` ⇔ `feature/<slug>` is merged into `main`. Check with
  `git branch --merged main`.
- Nothing outside the touched service's test projects, `e2e/`, and `.claude/plans/<slug>/` was
  modified by the tester, apart from the merge commit itself.
