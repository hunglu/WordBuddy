# Test report: Vocabulary Builder & Recall Check-up

## Backend unit/integration

**Content (`WordBuddy.Content.UnitTests`)** — 26/26 passing. Re-ran in this session:
`dotnet test src/Services/Content/WordBuddy.Content.slnx --filter "FullyQualifiedName~UnitTests"`
→ `Passed! - Failed: 0, Passed: 26, Skipped: 0, Total: 26`.

**Progress (`WordBuddy.Progress.UnitTests`)** — 10/10 passing. Re-ran in this session:
`dotnet test src/Services/Progress/WordBuddy.Progress.slnx --filter "FullyQualifiedName~UnitTests"`
→ `Passed! - Failed: 0, Passed: 10, Skipped: 0, Total: 10`.

**Content (`WordBuddy.Content.IntegrationTests`) / Progress (`WordBuddy.Progress.IntegrationTests`)**
— **Blocked, not run.** Both projects build cleanly as part of the `dotnet test` invocations above
(confirms `PersonalVocabularyEndpointsTests.cs` / `VocabularyRecallEndpointsTests.cs` compile
against the current app code). Actually executing them requires:
1. A reachable SQL Server (this sandbox has none — no LocalDB, and Docker Desktop's daemon is
   not running here: `docker ps` fails with
   `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine`).
2. The two pending EF Core migrations applied — `AddPersonalVocabularyWords` (Content) and
   `AddVocabularyRecall` (Progress) — via `dotnet ef database update`, which is `ask`-gated.

Neither was started in this session — see "Blocked — needs Sam" below.

## E2E API

**Written, not run against live services.** Added
`e2e/api/WordBuddy.E2E.Api.Tests/PersonalVocabularyTests.cs` (3 tests), following
`ServiceUrls.cs`/`ApiRequestContextFixture.cs`/`HealthCheckTests.cs` conventions:

- `AddRetrieveShareAndModerate_WordAppearsInSharedPool` — registers a fresh Adult learner via
  Identity, adds a word, retrieves it from `/api/vocabulary/mine`, requests share, then (as an
  admin) moderates/approves it with `VisibleToChildren = true`, and confirms it shows up in
  `/api/vocabulary/shared`.
- `ModerationEndpoint_NonAdminCaller_ReturnsForbidden` — a non-admin learner gets 403 from the
  moderation queue.
- `RequestShare_ChildCaller_ReturnsForbidden` — a Child-registered account gets 403 attempting
  `POST /{id}/share` (the `CanShareVocabulary` policy).

**JWT approach** (flagged per your instructions, since this needed a decision): this project has
no project reference to any service, so `Content.IntegrationTests`'s `TestJwtTokenFactory` (which
signs with an in-process `WebApplicationFactory` secret) isn't reusable here. Instead each test
hits Identity's real `/api/auth/register` (self-registers a unique-per-run learner, `Adult` or
`Child` age group as needed — no seeding gap here) and `/api/auth/login` for the admin token. The
admin token reuses the **existing** `admin@wordbuddy.com` / `Admin@123` dev seed
(`WordBuddy.Identity.Infrastructure/Seeding/DataSeeder.cs`, runs automatically in Development) —
no new test-user seeding step was needed, only override env vars
(`E2E_ADMIN_EMAIL`/`E2E_ADMIN_PASSWORD`) added for environments with different admin credentials.

**Confirmed compiles and is correctly wired**: `dotnet build e2e/api/WordBuddy.E2E.Api.Tests`
succeeds; `dotnet test e2e/api/WordBuddy.E2E.Api.Tests --filter "FullyQualifiedName~PersonalVocabularyTests"`
ran and failed cleanly with `Microsoft.Playwright.PlaywrightException: connect ECONNREFUSED
::1:5080` (Identity not running) — i.e. the only blocker is the missing live stack, not test code.
3/3 fail for that reason; 0 passed.

## E2E UI

**Written, not run against a live UI.** Added `e2e/ui/features/vocabulary.feature` (one scenario:
add a word → see it in the list → run and complete a recall check) and
`e2e/ui/steps/vocabulary.steps.ts`, following `login.feature`/`login.steps.ts`'s structure and
env-var credential pattern (`E2E_TEST_EMAIL`/`E2E_TEST_PASSWORD`, same test account).

**Confirmed wired correctly**: `npx bddgen` (from `e2e/ui/`) ran clean (exit 0) and generated
`.features-gen/features/vocabulary.feature.spec.js` — every Gherkin step in the new `.feature`
matched a step definition, alongside the pre-existing `login.feature`. Did not run
`npm test`/Playwright itself: that needs the UI + backend actually serving traffic at
`http://localhost:3000`, which isn't up in this session, and per your instructions I didn't start
it. (Pre-existing, unrelated to this change: `npx tsc --noEmit` in `e2e/ui` currently fails with
`TS2688: Cannot find type definition file for 'node'` — `@types/node` isn't installed even though
`tsconfig.json` references it; flagging since it means IDE/type-check tooling in this folder is
already broken independent of anything added here.)

## Blocked — needs Sam

To turn the above from "written and structurally verified" into actual pass/fail signal, three
things are needed, all `ask`-gated or requiring your environment:

1. **Docker Desktop needs to be started** — its daemon isn't running on this machine at all right
   now (`docker ps` → `dockerDesktopLinuxEngine` pipe not found), separate from the
   `docker compose up` permission gate itself.
2. **`docker compose up --build` from `WordBuddy/`** (`ask`-gated) — brings up SQL Server +
   all 5 APIs + the UI. A `.env` already exists in that folder (not read, per the safety rules,
   but its presence was confirmed), so this should be a matter of running it once Docker is up.
3. **`dotnet ef database update` for both new migrations** (`ask`-gated) —
   `AddPersonalVocabularyWords` (Content) and `AddVocabularyRecall` (Progress) — needed before
   either service's integration tests or any E2E flow touching vocabulary/recall data will work,
   even once the stack is up.

I did not attempt to work around any of these (e.g. no manual `docker compose up -d` retry, no
`dotnet ef database update`). Please confirm you want these three steps run, and I'll immediately
follow up with the actual integration-test and E2E runs once they're done.

## Failures

None in code — nothing has been executed against a live stack yet to fail. What's confirmed
**not** broken:
- All backend unit tests genuinely pass (re-run, not just trusted from the coder's report).
- Both integration test projects, the new E2E API test class, and the new E2E UI feature/steps
  all compile/generate cleanly against current app code — no wiring or syntax errors.
- The E2E API test's only failure mode observed is `ECONNREFUSED` (expected — no service
  listening), not an assertion failure or exception in test logic.

Once Docker + migrations are available, re-run:
```bash
dotnet test src/Services/Content/WordBuddy.Content.IntegrationTests
dotnet test src/Services/Progress/WordBuddy.Progress.IntegrationTests
dotnet test e2e/api/WordBuddy.E2E.Api.Tests
cd e2e/ui && npm test
```

## Sam's decision (2026-09-24)

Asked whether to apply the two pending migrations now (which turned out to require Docker +
`WordBuddy/.env`'s connection string regardless of scope, since no non-Docker local DB connection
is configured anywhere in this repo) — Sam chose to defer: leave integration/E2E tests as
written-and-compiling-but-unrun for now, rather than start Docker in this session. Nothing here
reflects a code defect; re-running the commands above once a live SQL Server is available is the
entire remaining path to a real pass/fail verdict.
