# Test report: WB-47 Admin lesson authoring

**Verdict: Approved — merged into main** (Run 2). Run 1 stopped: services were down.

## Run 1 — 2026-10-13 (stopped, services down)

Branch `feature/WB-47_admin-lesson-authoring`, HEAD `89703aa`. `origin/main` (`406f983`) already merged.

| Check | Result |
| --- | --- |
| Merge guard 1 (review approved, `3431dc9` is ancestor) | ✅ |
| Merge guard 2 (only allowlisted paths after review) | ✅ |
| Content unit tests | ✅ 309 passed |
| Content integration tests (LocalDB) | ✅ 120 passed (incl. new 409 test) |
| E2E API (`LessonAuthoringTests.cs`) | ⏸ Not run — ports 5080 / 5081 refused |
| E2E UI (`lesson-authoring.feature`) | ⏸ Not run — port 3000 refused |

### New test (uncommitted)

`LessonsAdminEndpointsTests.LessonRepository_SaveChanges_DuplicateLessonSenseRow_ReturnsSenseAlreadyAttached`
— covers review round 2 nit. Repository-level: the handler's in-memory check returns 409 before the
database, so a real race can only be simulated there.

### Found, not caused by WB-47

`e2e/ui` `npx tsc` fails: `tsconfig.json` sets `"types": ["node"]` but `@types/node` is not installed.
Existing defect on `main`.

## To finish

1. Sam starts the services (`make up` / `docker compose up`, ask-gated).
2. `npx playwright install` in `e2e/ui` if not done.
3. Re-run `/test WB-47_admin-lesson-authoring`.

## Run 2 — 2026-10-13

Branch `feature/WB-47_admin-lesson-authoring`, HEAD `89703aa`. `origin/main` (`406f983`) already merged ("Already up to date").

Stale-build check: Content image built 23:09, UI image 23:07 (after the 22:53 commit).
Swagger lists all `/api/lessons/admin*` routes; the UI bundle contains `admin/lessons`. `GET /api/lessons/admin` returns 401 (not 404 / 405).

### Merge guard

| Guard | Result |
| --- | --- |
| 1. Review Approve, `3431dc9` ancestor of HEAD | Pass |
| 2. Changed paths since review vs reviewed code + main | `.claude/plans/WB-47_admin-lesson-authoring/proposal.md`, `.../review.md` — all on allowlist |
| 3. Every suite ran, zero failures | Pass (see below) |

### Backend unit/integration

| Suite | Result |
| --- | --- |
| Content unit | 309 passed, 0 failed |
| Content integration (LocalDB) | 120 passed, 0 failed (final run, 30 s) |

Flaky note: two earlier full runs (3-4 min each, run in parallel with the E2E API run) had 2 and 3 failures, all in
`*MigrationTests`, each failing in 1 ms. The migration tests alone passed (16/16) and a clean full re-run passed 120/120.
Cause looks like LocalDB contention, not WB-47 code. Worth a separate look.

### E2E API

`LessonAuthoringTests.cs` — 2 passed, 0 failed.

### E2E UI

`lesson-authoring.feature` — 2 passed, 0 failed (chromium).

### Failures

None.

### Verdict

Approved and merged into main.
