# Test report: Batch stage commits and sync issue status

Tested on `feature/batch-stage-commits-and-sync-issue-status` at 37dd3be (`origin/main` merged in: already up to date). PR #9, issue #8.

## Backend unit/integration

Not applicable: workflow tooling only (`.claude/`, `docs/sdlc/`, root `CLAUDE.md`). No service code changed, so there are no service suites to run.

## E2E API

Not applicable: no API surface changed.

## E2E UI

Not applicable: no UI changed.

## Static consistency (run in this session)

- `.claude/settings.json` parses (`node JSON.parse`): 33 `ask` entries.
- Grep across `.claude/` and `docs/sdlc/` (excluding plan folders and the audit log) for "one commit per task", "record PR", separate "mark implemented" / "needs fixes" commits, and `git checkout <sha>^ --`: **no hits**. The only `git revert` mentions are the "not a `git revert`" notes (tester.md:186, test.md:53, workflow.md:115).
- Status to column mapping: github-integration.md table (idea→Ready, planned→Planned, in-progress/changes-requested/needs-fixes→In progress, implemented stays, reviewed→In review, done→Done, released→Released) matches workflow.md:25 and the targets in propose.md (Ready), plan.md (Planned), review.md (In review / In progress), test.md + tester.md (In progress / Done) and release.md (Released). **Consistent.**

## Verified in /test

- **a. Board "In review" option: NOT MET.** Read-only `gh project field-list 1 --owner hunglu` returns the Status options `Backlog, Ready, Planned, In progress, Done, Released`. **Counted as non-blocking:** this is a GitHub project setting, not a defect in this change. The docs already define report-and-skip for a missing option, and the card goes to Done on merge anyway. Sam still needs to add the option so that `/review` approve syncs correctly.
- **b. Ask-gates: verified statically only.** `settings.json` `ask` contains `Bash(git push:*)`, `PowerShell(git push:*)`, `Bash/PowerShell(gh issue edit:*)`, `Bash/PowerShell(gh project item-edit:*)`, `item-add:*` and `item-archive:*`. No board writes were run just to trigger prompts. **Still owed: someone needs to watch the prompts appear in both shells during a live run.**
- **c. Live run on this proposal: MET.** `git log --oneline main..HEAD`: under the new rules each stage run made one commit (`/code` first run 2b5810b implement, review round 1 25dfbe9, fix round 1 81023c3, review round 2 37dd3be). The 12 earlier per-task commits (9e757e1..cb376b4) were made before the new rules. Issue #8 is assigned to `hunglu`, and its project item Status is `In progress` (it would be In review if that option existed).

## Merge guard

1. review.md round 2: Approve, reviewed commit 81023c3. `git merge-base --is-ancestor 81023c3 HEAD` succeeds.
2. `git merge-tree --write-tree 81023c3 origin/main` (no conflict), then `git diff --name-only <tree> HEAD`:
   ```
   .claude/plans/batch-stage-commits-and-sync-issue-status/proposal.md
   .claude/plans/batch-stage-commits-and-sync-issue-status/review.md
   ```
   Both paths are on the allowlist.
3. No suites apply (see above). The static verification all passed.

## Failures

None. Open items that don't block the merge: (a) the board has no "In review" option; (b) the live ask-prompt check is still owed.

## Verdict

Approved — merged into main
