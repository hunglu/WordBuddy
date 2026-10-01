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

2. **Make sure a PR exists.** If `pr:` is `none`/missing, create it the way `/code` step 6 does
   (ask-gated) and record `pr: <n>` before reviewing.

3. **Invoke the `reviewer` subagent** (`subagent_type: reviewer`) with the slug, `proposal.md`,
   `plan.md` and `tasks.md`. It writes `review.md`, posts it on the PR, sets the status, commits
   and pushes (pushes and PR posts are ask-gated — Sam approves each).

4. **Report back**: verdict, count of blockers / majors / nits, and the top findings.
   - `reviewed` → next is `/test <slug>`.
   - `changes-requested` → next is `/code <slug>` to fix the findings (the coder reads
     `review.md`); then `/review <slug>` again.

## Verification

- `.claude/plans/<slug>/review.md` exists with a verdict and the reviewed commit sha.
- `proposal.md` status is `reviewed` or `changes-requested`, with `version`/`updated` bumped.
- No application code changed.
