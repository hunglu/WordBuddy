# Plan: Add pull request and code review stage

Retro-written to describe what was implemented, so the reviewer can check the diff against it.

## Approach

- New subagent `.claude/agents/reviewer.md`: reads `git diff origin/main...origin/feature/<slug>`
  and the PR, checks correctness → security/privacy → plan conformance → repo conventions →
  testability → maintainability; writes `review.md`; posts with `gh pr review --comment`; sets
  status; commits and pushes. Never edits application code, never merges.
- New command `.claude/commands/review.md` (stage 4 of 6) that validates `implemented`, makes
  sure a PR exists, invokes the reviewer, reports the verdict.
- `/code`: new step 6 opens the PR (or comments on it in a fix round) and records `pr:`; fix
  rounds take `review.md` / `test-report.md` as the task list; accepted statuses extended.
- `/test` + tester: require `reviewed`; merge via `gh pr merge <pr> --merge` with `Closes #n`,
  local `--no-ff` only as fallback when no PR exists; post the test report on the PR.
- `/status`: statuses `changes-requested`, `reviewed`; PR column; warn on `pr: none`.
- `/propose` template: `pr: none`.
- `.claude/settings.json`: ask-gate `gh pr merge|review|comment|edit` (Bash + PowerShell).
- Docs: `docs/sdlc/workflow.md` diagram/table/gates, `docs/sdlc/github-integration.md` board
  column "In review" and daily loop, root `CLAUDE.md` workflow table.

## Files

`.claude/agents/reviewer.md` (new), `.claude/commands/review.md` (new), `.claude/commands/code.md`,
`.claude/commands/test.md`, `.claude/commands/status.md`, `.claude/commands/propose.md`,
`.claude/agents/tester.md`, `.claude/settings.json`, `CLAUDE.md`, `docs/sdlc/workflow.md`,
`docs/sdlc/github-integration.md`.

## Risks

- Self-approval impossible on GitHub → verdict by comment only.
- `/test` exception for re-tests without code changes must not become a way to skip review.
