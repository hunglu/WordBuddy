---
title: Code — implement an approved plan
description: Stage 3 of the idea → plan → code → test workflow. Invokes the coder subagent to implement tasks from an approved plan. Refuses to run without an approved plan.
status: draft
---

## Context

Stage 3 of 4: **propose → plan → code → test**. Sam running this command *is* the approval —
there is no separate "approve" step beyond having reviewed `plan.md`/`tasks.md` from `/plan` and
choosing to invoke `/code`. Implementation is done by the `coder` subagent
(`.claude/agents/coder.md`).

Argument: `$ARGUMENTS` is the slug. If missing, list folders under `.claude/plans/` with
`status: planned` (or `in-progress`, for resuming) and ask Sam which one.

## Steps

1. **Validate.** Confirm `.claude/plans/<slug>/plan.md` and `tasks.md` exist and
   `proposal.md`'s status is `planned` or `in-progress`. If status is still `idea`, refuse and
   tell Sam to run `/plan <slug>` first. Never fabricate a plan to get around this check.

2. If status is `planned`, edit `proposal.md` frontmatter to `status: in-progress` before
   starting.

3. **Invoke the `coder` subagent** (via the Agent tool, `subagent_type: coder`), passing it the
   full contents of `plan.md` and `tasks.md` and the slug. Tell it to work through unchecked
   tasks in `tasks.md` in order, checking each one off as it lands.

4. **If the coder subagent stops partway** because a task needs an `ask`-gated command
   (migrations, docker compose, git push — see `.claude/settings.json`) or hits a genuine
   ambiguity the plan didn't cover: relay that to Sam plainly, leave `tasks.md` exactly as the
   subagent left it (partial progress is fine and expected), and leave `proposal.md` at
   `status: in-progress` — don't mark it `implemented` on a partial run.

5. **Once every task in `tasks.md` is checked off**, edit `proposal.md` frontmatter to
   `status: implemented`.

6. **Report back** with what was implemented (files touched, per the coder's own summary) and
   tell Sam the next step is `/test <slug>`.

## Verification

- Every `- [ ]` in `tasks.md` that the subagent claims to have completed is actually `- [x]`.
- `dotnet build`/`npm run build` succeed for whatever the coder touched (the coder subagent is
  responsible for checking this per task — spot-check if in doubt).
- `proposal.md` status only reaches `implemented` when `tasks.md` has no remaining `- [ ]` items.
