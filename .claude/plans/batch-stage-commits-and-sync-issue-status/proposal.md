---
title: Batch stage commits and sync issue status
status: changes-requested
type: change
issue: 8
pr: 9
affects: none
supersedes: none
version: 1.4
created: 2026-10-01T17:37:59+07:00
updated: 2026-10-01T22:01:54+07:00
---

## Problem

Sam's request: "fix commit many times during a stage and do not change status of Issue and
assign user before starting work on issue."

1. **Too many commits per stage.** Each workflow stage makes several small bookkeeping commits on
   the feature branch. In the `2-navigation-items-have-selected-state` run, `/code` alone made
   "mark implemented" and then a separate "record PR #7" commit, each with its own push. Other
   stages add "review round N", "needs fixes" and "mark done" commits. This clutters the branch
   history and asks Sam to approve a push for each one.
2. **The GitHub issue isn't updated when work starts.** When `/code` starts on an issue, the
   issue's status on the `WordBuddy` project board doesn't change (e.g. to In Progress) and no one
   is assigned. The board doesn't show what is being worked on, or by whom.

## Goal

- Each stage writes its bookkeeping (status, version, PR number, reports) in as few commits as
  possible, ideally one per stage per cycle, with one push per stage per cycle. The cycle mean when coder start implement all tasks until finish it, then commit to feature branch, if any needed fixes, do the fix then commit once. 
- When work on an issue starts, the issue is assigned to the user and moved to the matching
  status on the project board. Later stages keep the board status in step with `status:` in
  `proposal.md`.

## Target users

Neither children nor adults. This changes the development workflow only, not the app.

## Success criteria

- A full propose → test run produces at most one bookkeeping commit and one push per stage, plus
  the coder's one-commit-per-task commits.
- After `/code` starts, the linked issue is assigned and shows In Progress on the board.
- The board status follows the workflow status through review, test and done.
- Every push, issue edit and board update stays ask-gated.

## Constraints

_Not specified._ Which user gets assigned, and the exact mapping of board columns to `status:`
values, are to be settled in `/plan`.
