# Review: Batch stage commits and sync issue status

PR: #9 · Round 2 · Reviewed commit: 81023c3 · 2026-10-01T22:31:00+07:00

## Verdict
Approve. All round-1 findings are resolved and the fix round adds no new inconsistencies.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | .claude/plans/batch-stage-commits-and-sync-issue-status/tasks.md:51-52 | The approved bullet text has a nested backtick span (`` `status:` ``) inside an outer code span, so its literal form is ambiguous. CLAUDE.md:68 renders it as `status:` in backticks with the path unquoted, which is the only sensible reading. | None. |

### Round-1 verification
- **Major 1: resolved.** workflow.md:41-42, test.md Verification ("Commits added by the run, by case"), workflow.md "A conflicting PR is left as it is" and tester.md all describe the same two cases. A local `git merge origin/main` conflict means no commit and no push. A PR that is `CONFLICTING`/`UNKNOWN` after the push gets `mark done` followed by `undo mark done`. `git grep` finds no "report-only" or "report only" left outside the plan folders.
- **Major 2: resolved.** All three places (tester.md:190, test.md:52, workflow.md:114) now use `git restore --source=<sha>^ --staged --worktree -- .claude/plans/<slug>/proposal.md docs/features/`. No `git checkout <sha>^ --` form remains. I checked this in a scratch repo: a file added by the later commit is deleted and staged (`D d/b`). The undo commit only touches `proposal.md`, `docs/features/**` and `test-report.md`, which are all on the merge-guard allowlist (workflow.md:94-96).
- **Nit 1 (wording): resolved.** All three files say "the one documented exception to one commit per cycle". The "not counted as bookkeeping" phrasing is gone.
- **Nit 2 (`pr:` reset): addressed** with a note in tester.md:198-199.
- **Nit 3 (CLAUDE.md bullet): resolved.** CLAUDE.md:68 is now one line with an unquoted path, matching tasks.md.

## Plan conformance
- All 10 tasks are still covered (see round 1). The fix round touched only tester.md, test.md, workflow.md, CLAUDE.md and proposal.md, all within the round-1 findings.
- Board: the project-1 Status options are still `Backlog · Ready · Planned · In progress · Done · Released`. **`In review` is missing**, so the approve sync is reported and skipped. Sam needs to add the option.

## Previous rounds
### Round 1 — Batch stage commits and sync issue status

PR: #9 · Round 1 · Reviewed commit: 2b5810b · 2026-10-01T22:01:54+07:00

#### Verdict
Changes requested — two majors in the `/test` failure path: the docs describe an outcome that no longer exists, and the "undo mark done" step leaves newly created living specs behind.

#### Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | docs/sdlc/workflow.md:41, .claude/commands/test.md (Verification, "report-only") | The cycle table still lists `/test` PR conflicting → "report only, status unchanged", and test.md's Verification still accepts a "report-only" commit. tester.md no longer has that path. If `git merge origin/main` conflicts, it commits nothing. If GitHub reports `CONFLICTING`/`UNKNOWN`, the run has already pushed `tests, report, mark done` and then adds `undo mark done — <reason>`, so it makes 2 commits and 2 pushes. Scenario: someone checking a conflicting `/test` run against workflow.md expects one report-only commit and finds two commits, so the docs and the agent disagree. | Replace the row with two rows: "`main` merge conflicts locally → no commit" and "PR not mergeable after push → `tests, report, mark done` + `undo mark done — <reason>`". Drop "report-only" from test.md Verification. |
| 2 | major | .claude/agents/tester.md (Undo block, `git checkout <done sha>^ -- .claude/plans/<slug>/proposal.md docs/features/`); same recipe in test.md step 5 and workflow.md:110 | `git checkout <tree-ish> -- <dir>` only overwrites paths that exist in the source. It does not delete files the done commit **added**. Step 3 creates `docs/features/<feature>.md` from `_template.md` when it's missing (with `state: shipped`). After the undo, that file is still on the branch even though nothing merged. This breaks test.md's guarantee that "`done` is never left on `feature/<slug>` without the merge". | Use `git restore --source=<done sha>^ --staged --worktree -- .claude/plans/<slug>/proposal.md docs/features/`. It removes tracked files that are absent in the source, and it's the same form as the declined-push path. Update all three places. |
| 3 | nit | docs/sdlc/workflow.md:46-50 vs .claude/agents/tester.md (Commit section) | workflow.md says the undo commit is "not counted as a bookkeeping commit". tester.md calls it "the one documented exception to one-commit-per-cycle". The two say the same thing in different ways. | Pick one wording in both files. |
| 4 | nit | .claude/agents/tester.md (Undo) | The path-limited restore of `proposal.md` also reverts a `pr:` value that `/test` itself resolved via `gh pr list --head`. This is harmless because `pr:` is a cache, but it's unstated. | Optionally re-apply `pr: <n>` after the restore, or mention it. |
| 5 | nit | CLAUDE.md:68-69 | The added bullet is wrapped over two lines and puts the path in backticks. The approved text in tasks.md is one line with an unquoted path. The content is identical and the `/code` row matches exactly. | None needed. Note it for Sam, since the text was approval-gated. |

#### Plan conformance
- All 10 tooling tasks are reflected in the diff: github-integration Board sync section and mapping, workflow.md cycle rule and lagging `pr:`, coder makes no commits or pushes, code.md single commit/push and start sync, reviewer/review.md lookup and sync, tester/test.md batched order with push before the mergeability check, propose/plan/release sync, status.md fallback.
- The mapping table is identical in plan.md, github-integration.md and every command. Status names are consistent.
- I found no leftover per-task commit, "record PR", separate "mark implemented"/"needs fixes"/"mark done" commit instructions. The only `git revert` mentions are the "not a `git revert`" notes.
- Merge-guard allowlist (workflow.md:93-95) covers everything in the tester's single commit and in the undo commit: tests, `e2e/**`, `test-report.md`, `proposal.md`, `docs/features/**` (including README row).
- `.claude/settings.json` only adds `ask` entries (7). The JSON parses. `gh issue edit`, `gh pr *` and `git push` are gated in both shells.
- Board: the Status field (project 1) has `Backlog · Ready · Planned · In progress · Done · Released`. **`In review` is still missing**, so an approve verdict would hit the report-and-skip rule.
- Out of scope: none. Already disclosed in the PR: commits made under the old per-task rule, and the Co-Authored-By trailer on 9e757e1.
