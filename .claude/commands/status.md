---
title: Status — show the workflow board
description: Lists every proposal under .claude/plans/ with its status, type, and linked issue, and suggests the next command for each. Read-only.
---

## Context

The local tracker for the **propose → plan → code → review → test → release** workflow.
Read-only: never edits any file, never changes a status.

## Steps

1. For each folder in `.claude/plans/`, read `proposal.md` frontmatter (`title`, `status`, `type`,
   `issue`, `pr`, and for blocked ones `blocked-from`, `blocked-by`). A folder without
   `proposal.md` (e.g. a one-off `notes.md`) is listed as `untracked`.
2. Print one markdown table grouped by status, in this order:
   `needs-fixes`, `changes-requested`, `reviewed`, `implemented`, `in-progress`, `planned`,
   `idea`, `blocked`, `released`, `done`, `untracked`.
   Columns: slug · title · type · issue · PR · next step.
   Under the table, warn about: any issue number used by more than one proposal (a duplicate
   link — Sam picks which folder keeps it), and every proposal with `issue: pending` or `none`
   (not yet on the board).
3. Next step per status:
   | status | next |
   |---|---|
   | idea | `/plan <slug>` |
   | planned | review `plan.md`, then `/code <slug>` |
   | in-progress | `/code <slug>` (resume) |
   | implemented | `/review <slug>` |
   | changes-requested | `/code <slug>` (fix review findings), then `/review <slug>` |
   | reviewed | `/test <slug>` |
   | needs-fixes | code failure → `/code <slug>`, then `/review` → `/test`; test-only failure (services down, flaky test) → `/test <slug>` again (the merge guard decides) |
   | done | `/release` when ready to ship |
   | blocked | show `blocked-by`; once resolved, Sam sets `status` back to `blocked-from` (see `docs/sdlc/workflow.md` → Blocked) |
4. End with one line recommending the single most useful next action (fix before build before
   plan: `needs-fixes` > `changes-requested` > `reviewed` > `implemented` > `in-progress` >
   `planned` > `idea`). Also warn when an `implemented`+ proposal has `pr: none`.
