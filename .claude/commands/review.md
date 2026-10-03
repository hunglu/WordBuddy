---
title: Review — code review of the pull request
description: Stage 4 of the propose → plan → code → review → test → release workflow. Invokes the reviewer subagent to review the feature/<slug> pull request against the plan and repo conventions, writes review.md, and posts the review on GitHub. Never changes application code.
---

## Context

Stage 4 of 6: **propose → plan → code → review → test → release**. `/code` opened the pull request
`feature/<slug>` → `main` and recorded its number in `proposal.md` (`pr:`). This command gets a
review on it before `/test` is allowed to merge.

Argument: `$ARGUMENTS` is the slug. If missing, list proposals with `status: implemented` and ask
Sam which one.

## Steps

1. **Validate.** `proposal.md` status is `implemented` (first review, or re-review after
   `/code` fixed a `changes-requested` or `needs-fixes` round). Otherwise tell Sam the right
   command (`/code <slug>` first). `feature/<slug>` exists on `origin`.

2. **Make sure a PR exists.** `pr:` may lag one stage (see `docs/sdlc/workflow.md`). If it is
   `none`/missing, look it up first:
   `gh pr list -R hunglu/WordBuddy --head feature/<slug> --state all --json number --jq '.[0].number'`.
   Only if that finds nothing, create it the way `/code` step 6 does (ask-gated). Either way write
   `pr: <n>` to `proposal.md` in the working tree — the reviewer's single commit carries it.

3. **Invoke the `reviewer` subagent** (`subagent_type: reviewer`) with the slug, `proposal.md`,
   `plan.md` and `tasks.md`. It writes `review.md`, posts it on the PR, sets the status, makes
   the round's single commit and push, and runs board sync (In review / In progress). Pushes, PR
   posts and board writes are ask-gated — Sam approves each. A reusable lesson (e.g. a repeated
   finding) is appended to `docs/ai/learnings.md` in the same commit — never to a `CLAUDE.md`.

4. **Report back**: verdict, count of blockers / majors / nits, the top findings, the commit sha
   and the board sync result.
   - `reviewed` → next is `/test <slug>`.
   - `changes-requested` → next is `/code <slug>` to fix the findings (the coder reads
     `review.md`); then `/review <slug>` again.

## Verification

- `.claude/plans/<slug>/review.md` exists with a verdict and the reviewed commit sha.
- `proposal.md` status is `reviewed` or `changes-requested`, with `version`/`updated` bumped.
- No application code changed.
- Exactly one commit (`<slug>: review round <n>`) and one push this round.
- Board Status matches the verdict (In review / In progress), or the skip reason is reported.

## Scope suggestions

If the subagent report has a `## Scope suggestions` list, post it as **one issue comment** before reporting back (`docs/sdlc/github-integration.md` → Scope suggestions; `gh issue comment`, ask-gated). Never edit the issue body or title. Include the suggestions in the report to Sam.
