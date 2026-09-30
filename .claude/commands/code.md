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
   starting. Every edit to `proposal.md` also bumps `version` and sets `updated` (see
   `docs/sdlc/workflow.md` → Proposal versioning).

3. **Invoke the `coder` subagent** (via the Agent tool, `subagent_type: coder`), passing it the
   full contents of `plan.md` and `tasks.md` and the slug. Tell it to first create (or resume)
   the `feature/<slug>` branch and publish it to GitHub, as described in
   `.claude/agents/coder.md`. It then works through unchecked tasks in `tasks.md` in order,
   checking each one off and committing it to that branch as it lands.

4. **If the coder subagent stops partway** because a task needs an `ask`-gated command
   (migrations, docker compose, git push — see `.claude/settings.json`) or hits a genuine
   ambiguity the plan didn't cover: relay that to Sam plainly, leave `tasks.md` exactly as the
   subagent left it (partial progress is fine and expected), and leave `proposal.md` at
   `status: in-progress` — don't mark it `implemented` on a partial run.

5. **Once every task in `tasks.md` is checked off**, edit `proposal.md` frontmatter to
   `status: implemented` (bump `version` + `updated`, as every proposal edit does). Confirm you're on `feature/<slug>` (`git branch --show-current`), then
   commit `.claude/plans/<slug>/` (`git add .claude/plans/<slug>` then
   `git commit -m "<slug>: mark implemented"`, ending with the Co-Authored-By line) and
   `git push origin feature/<slug>` (ask-gated, so Sam approves it). On a partial run, commit and
   push whatever the coder finished the same way so the branch on GitHub reflects progress.

6. **Report back** with what was implemented (files touched, per the coder's own summary), the
   branch name, and whether it's pushed. Tell Sam the next step is `/test <slug>`, which tests
   the branch and merges it into `main` if everything passes.

## Verification

- Every `- [ ]` in `tasks.md` that the subagent claims to have completed is actually `- [x]`.
- `dotnet build`/`npm run build` succeed for whatever the coder touched (the coder subagent is
  responsible for checking this per task — spot-check if in doubt).
- `proposal.md` status only reaches `implemented` when `tasks.md` has no remaining `- [ ]` items.
- All commits are on `feature/<slug>`. `main` has no new commits from this run.
