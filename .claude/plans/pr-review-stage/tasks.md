# Tasks: pr-review-stage

- [x] Add `reviewer` subagent (`.claude/agents/reviewer.md`).
- [x] Add `/review` command (`.claude/commands/review.md`).
- [x] `/code` opens/updates the PR, records `pr:`, supports fix rounds.
- [x] `/test` requires `reviewed`; tester merges through the PR.
- [x] `/status` new statuses, PR column, `pr: none` warning.
- [x] `/propose` template gets `pr: none`.
- [x] Ask-gate `gh pr merge|review|comment|edit` in `.claude/settings.json`.
- [x] Update `docs/sdlc/workflow.md`, `docs/sdlc/github-integration.md`, root `CLAUDE.md`.

## Tests

- [ ] `settings.json` parses; every command/agent referenced exists; status names are consistent
      across `code.md`, `review.md`, `test.md`, `status.md`, `workflow.md`.
- [ ] Dry run: `/status` renders the new statuses and PR column.
