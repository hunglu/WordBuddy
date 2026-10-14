# Test report: WB-48 Edit own vocabulary word

**Verdict: Stopped — not merged** (Run 1). Services were down; E2E could not run.

## Run 1 — 2026-10-14 (stopped, services down)

Branch `feature/WB-48_edit-own-vocabulary-word`, HEAD `253cce8`. `origin/main` already merged ("Already up to date").

| Check | Result |
| --- | --- |
| Merge guard 1 (review approved, `72b21d1` is ancestor) | ✅ |
| Merge guard 2 (only allowlisted paths after review) | ✅ |
| Content unit tests | ✅ 331 passed |
| Content integration tests (LocalDB) | ✅ 129 passed |
| E2E API (`EditVocabularyWordTests.cs`) | ⏸ Not run — port 5081 refused |
| E2E UI (`vocabulary-edit.feature`) | ⏸ Not run — UI not running |

Stale-build check: `PUT /api/vocabulary/<guid>` got no response (HTTP 000). `docker ps` shows only the kind control plane.

## To finish

1. Sam rebuilds and starts Content and the UI from this branch (`docker compose up -d --build`, ask-gated).
2. Unauthenticated `PUT /api/vocabulary/<guid>` returns 401, not 405.
3. `npx playwright install` in `e2e/ui` if not done.
4. Re-run `/test WB-48_edit-own-vocabulary-word`.
