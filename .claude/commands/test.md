---
title: Test — verify an implemented plan
description: Stage 4 of the idea → plan → code → test workflow. Invokes the tester subagent to write/extend unit, integration, and E2E tests and report results.
status: draft
---

## Context

Stage 4 of 4: **propose → plan → code → test**. Verification is done by the `tester` subagent
(`.claude/agents/tester.md`), which owns unit/integration tests inside each touched service's
existing `UnitTests`/`IntegrationTests` projects, plus the cross-cutting E2E projects
`e2e/api/WordBuddy.E2E.Api.Tests` (.NET Playwright, API-level) and `e2e/ui` (TypeScript
Playwright + playwright-bdd, UI-level).

Argument: `$ARGUMENTS` is the slug. If missing, list folders under `.claude/plans/` with
`status: implemented` and ask Sam which one.

## Steps

1. **Validate.** Confirm `.claude/plans/<slug>/plan.md` and `tasks.md` exist and `proposal.md`'s
   status is `implemented` (or `needs-fixes`, for a re-run after fixes). If status is earlier
   than that, tell Sam to run `/code <slug>` first.

2. **Invoke the `tester` subagent** (via the Agent tool, `subagent_type: tester`), passing it
   `plan.md` (to know what was built and which service(s)/frontend areas it touched) and the
   slug.

3. The tester subagent writes and runs tests, and reports pass/fail. If it needed to run
   anything `ask`-gated (`docker compose up` to have live services for E2E, for example), it
   should have stopped and asked Sam directly rather than working around it — if it reports that
   happened, relay it to Sam plainly.

4. **Write `.claude/plans/<slug>/test-report.md`** summarizing what ran and the outcome (this can
   be done by the tester subagent directly, or by you from its report — either way it must exist
   after this command).

5. **Update `proposal.md` frontmatter**: `status: done` if everything passed, `status: needs-fixes`
   if anything failed — and in the failing case, summarize the failures for Sam so they know
   whether to loop back to `/code <slug>` or `/plan <slug>`.

## Verification

- `.claude/plans/<slug>/test-report.md` exists and states a clear pass/fail outcome.
- `proposal.md` status is `done` or `needs-fixes`, never left at `implemented`.
- Nothing outside the touched service's test projects, `e2e/`, and `.claude/plans/<slug>/` was
  modified.
