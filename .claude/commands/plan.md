---
title: Plan — analyze a proposal and produce a technical plan
description: Stage 2 of the idea → plan → code → test workflow. Invokes the planner subagent to turn a captured proposal into an approved-pending technical plan and task list. Never writes application code.
status: draft
---

## Context

Stage 2 of 4: **propose → plan → code → test**. This command turns `.claude/plans/<slug>/proposal.md`
into `plan.md` + `tasks.md` via the `planner` subagent (`.claude/agents/planner.md`). This is the
**approval gate** of the whole workflow: after this command finishes, implementation must not
start until Sam explicitly runs `/code <slug>`.

Argument: `$ARGUMENTS` is the slug. If missing, list the folders under `.claude/plans/` that have
`status: idea` and ask Sam which one.

## Steps

1. **Validate.** Confirm `.claude/plans/<slug>/proposal.md` exists. If not, tell Sam to run
   `/propose` first — do not invent a proposal from nothing.

2. **Invoke the `planner` subagent** (via the Agent tool, `subagent_type: planner`), passing it:
   - The full contents of `.claude/plans/<slug>/proposal.md`
   - The slug and the literal instruction to write its output to
     `.claude/plans/<slug>/plan.md` and `.claude/plans/<slug>/tasks.md`

   Do not do the analysis yourself in the main thread — the planner subagent owns this so its
   context stays scoped to "understand the codebase and design an approach," separate from the
   conversational back-and-forth of the rest of the workflow.

3. **After the planner returns**, edit `.claude/plans/<slug>/proposal.md`'s frontmatter:
   `status: idea` → `status: planned`. Every edit to `proposal.md` also bumps `version` and sets
   `updated` (see `docs/sdlc/workflow.md` → Proposal versioning).

4. **Report back** with a short summary of the plan (not the full file — Sam can read
   `plan.md`) and the task count from `tasks.md`. End with: implementation will **not** start
   automatically — Sam runs `/code <slug>` once they've reviewed and are happy with the plan.

## Verification

- `.claude/plans/<slug>/plan.md` and `tasks.md` exist and are non-empty.
- `proposal.md` frontmatter now reads `status: planned`.
- No file outside `.claude/plans/<slug>/` was modified.
