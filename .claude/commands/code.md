---
title: Code — implement an approved plan
description: Stage 3 of the propose → plan → code → review → test → release workflow. Invokes the coder subagent to implement tasks from an approved plan. Refuses to run without an approved plan.
status: draft
---

## Context

Stage 3 of 6: **propose → plan → code → review → test → release**. Sam running this command *is* the approval —
there is no separate "approve" step beyond having reviewed `plan.md`/`tasks.md` from `/plan` and
choosing to invoke `/code`. Implementation is done by the `coder` subagent
(`.claude/agents/coder.md`).

Argument: `$ARGUMENTS` is the slug. If missing, list folders under `.claude/plans/` with
`status: planned` (or `in-progress`, for resuming) and ask Sam which one.

## Steps

1. **Validate.** Confirm `.claude/plans/<slug>/plan.md` and `tasks.md` exist and
   `proposal.md`'s status is `planned` or `in-progress` (or `changes-requested` / `needs-fixes`
   for a fix round — see the end of Steps). If status is still `idea`, refuse and
   tell Sam to run `/plan <slug>` first. Never fabricate a plan to get around this check.

2. **Start: status + board sync.** If status is `planned`, edit `proposal.md` frontmatter to
   `status: in-progress` before starting. Every edit to `proposal.md` also bumps `version` and
   sets `updated` (see `docs/sdlc/workflow.md` → Proposal versioning). Don't commit it yet — it
   goes into this run's single commit. Then run **Board sync**
   (`docs/sdlc/github-integration.md` → Board sync):
   - first run: `gh issue edit <issue> -R hunglu/WordBuddy --add-assignee @me` (skip if already
     assigned to `@me`), then set Status **In progress**;
   - fix round: set Status **In progress** only.
   Both are ask-gated; skip when `issue:` is `none`/`pending`. If `gh`/scope is missing or Sam
   declines, say so and continue — board sync never blocks the run.

3. **Invoke the `coder` subagent** (via the Agent tool, `subagent_type: coder`), passing it the
   full contents of `plan.md` and `tasks.md` and the slug. It creates (locally, not pushed) or
   resumes `feature/<slug>`, works through unchecked tasks in order, checks each off in
   `tasks.md`, and makes **no** commits or pushes. It returns the list of files it touched.

4. **If the coder subagent stops partway** because a task needs an `ask`-gated command
   (migrations, docker compose — see `.claude/settings.json`) or hits a genuine ambiguity the
   plan didn't cover: relay that to Sam plainly, leave `tasks.md` exactly as the subagent left it
   (partial progress is fine and expected), and leave `proposal.md` at `status: in-progress` —
   don't mark it `implemented` on a partial run.

5. **One commit + one push for the run.** If every task in `tasks.md` is checked off, edit
   `proposal.md` to `status: implemented` (bump `version` + `updated` — still the same edit
   session as step 2, so a first run gets one bump in total if nothing else changed in between).
   Confirm you're on `feature/<slug>` (`git branch --show-current`), then:
   ```bash
   git add <each file the coder reported> .claude/plans/<slug>   # never git add -A / .
   git commit -m "<message>"                                      # no Co-Authored-By trailer
   git push -u origin feature/<slug>                              # ask-gated
   ```
   `<message>` is `<slug>: implement` (first run, all tasks done), `<slug>: fix round <n>` (fix
   round), or `<slug>: wip (<k>/<n> tasks)` (stopped/partial run, status stays `in-progress`).
   That is the run's only commit and only push. If Sam declines the push, say the branch still
   needs to be pushed and stop before the PR step.

6. **Open or update the pull request** (only once `status: implemented`, not on a partial run):
   - Resolve the PR first: if `pr:` is `none`/missing, check
     `gh pr list -R hunglu/WordBuddy --head feature/<slug> --state all --json number --jq '.[0].number'`
     (`pr:` may lag one stage — see `docs/sdlc/workflow.md`).
   - No PR yet → create it (ask-gated):
     ```bash
     gh pr create -R hunglu/WordBuddy --base main --head feature/<slug> \
       --title "<title>" --body-file <tmp body>
     ```
     Body (temp file in the scratchpad): `Plan: .claude/plans/<slug>/`, a short summary of what
     changed (from the coder's report), the task checklist status, and `Closes #<issue>` (omit
     when `issue` is none/pending). Write `pr: <n>` to `proposal.md` **in the working tree only**
     — no commit, no push, no extra version bump (same edit session). The next stage's commit
     carries it.
   - PR exists (fix round after review or test) → `gh pr comment <pr> --body "<what was fixed,
     which findings>"` (ask-gated).
   If `gh` is unavailable or Sam declines, leave `pr: none`, say so; `/review` can open it later.

7. **Report back** with what was implemented (files touched, per the coder's own summary), the
   branch name, the commit sha, whether it's pushed, the PR link, and the board sync result. Tell
   Sam the next step is `/review <slug>`, which reviews the PR before `/test` can merge it.

**Fix rounds.** If status is `changes-requested` (from `/review`) or `needs-fixes` (from
`/test`), pass `review.md` / `test-report.md` to the coder as the task list for this round; it
fixes only those findings on the same branch. Status goes back to `implemented` when done, in the
round's single `fix round <n>` commit. An uncommitted `pr:` edit in the working tree is expected
and doesn't block the round.

## Verification

- Every `- [ ]` in `tasks.md` that the subagent claims to have completed is actually `- [x]`.
- `dotnet build`/`npm run build` succeed for whatever the coder touched (the coder subagent is
  responsible for checking this per task — spot-check if in doubt).
- `proposal.md` status only reaches `implemented` when `tasks.md` has no remaining `- [ ]` items.
- All commits are on `feature/<slug>`. `main` has no new commits from this run.
- This run added exactly one commit and made one push (`git log --oneline` before/after; a local
  `git merge origin/main` for conflict resolution isn't counted).
- Board sync ran (or the reason it was skipped is reported).
