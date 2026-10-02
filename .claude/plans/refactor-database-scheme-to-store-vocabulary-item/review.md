# Review: Refactor database scheme to store vocabulary item

PR: #11 · Round 1 · Reviewed commit: b60769d · 2026-10-02T17:37:59+07:00

## Verdict
Changes requested — two majors: a concurrent add/link of the same word now surfaces as an unhandled 500, and the migration can link a Child learner to a word that is not child-visible. That second point breaks the child rule the runtime handlers follow.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `WordBuddy/src/Services/Content/WordBuddy.Content.Infrastructure/Repositories/VocabularyWordRepository.cs:384-411` (callers `AddPersonalVocabularyWordCommandHandler.cs:81,113`, `AddSharedVocabularyWordToMyListCommandHandler.cs:78`) | Dedupe now relies on two unique indexes, `UX_VocabularyWords_ContentHash_OwnerUserId_Learner` and `IX_UserVocabularyWords_UserId_VocabularyWordId`. Each handler does check-then-insert with no conflict handling, and nothing in Content catches `DbUpdateException`. Scenario: a learner double-clicks "Add" (or two tabs adopt the same shared word). Both requests see no word or link, both insert, and the second hits a unique-key violation. The global middleware turns that into a 500. Before this PR duplicates were allowed, so the same action returned 201 twice. This is a new failure on a public endpoint. | Catch the unique violation in `AddAsync`/`LinkAsync` (SQL error 2601/2627 on `DbUpdateException`). Then re-read and return the existing word or link id, which matches the idempotent contract the handlers already document. Add one integration test that runs two parallel adds of the same word. |
| 2 | major | `WordBuddy/src/Services/Content/WordBuddy.Content.Infrastructure/Persistence/Migrations/20261002092015_UnifyVocabularyWords.cs:192-197` | The "adopted copy" collapse joins a Private/Rejected row to another owner's `Shared` row only by hash. It ignores the copy owner's age group and the shared row's `VisibleToChildren`. Scenario: Child C privately wrote "bank / … / …". Adult A's identical word was approved with `VisibleToChildren = false`. The migration deletes C's row, links C (`IsAuthor = 0`) to A's non-child-visible word, and emits a remap. At runtime the same add would **not** link, because `PickVisibleDuplicate`/`IsVisibleTo` excludes it. The plan says "Dedupe-on-add never links a Child … to a word they couldn't see". The content is identical, so nothing new is shown to C. Still, it breaks the stated child invariant and C loses authorship of their own word. | Carry `OwnerAgeGroup` into `#Learner` (and `VisibleToChildren` for the shared side). Add `AND (s.[OwnerAgeGroup] <> N'Child' OR o.[VisibleToChildren] = 1)` to the `sh` apply. Extend `UnifyVocabularyWordsMigrationTests` with a Child private row vs. a non-child-visible shared row: the Child row must be kept, with no remap. |
| 3 | nit | `.../Migrations/20261002092015_UnifyVocabularyWords.cs:157-198` | `#Learner` has an index only on `Id`. The `sh` OUTER APPLY scans `#Learner` by `ContentHash` once per survivor, and the `sys` apply scans `VocabularyWords` before `IX_VocabularyWords_ContentHash` exists (indexes are created after the data). That is quadratic on a large `PersonalVocabularyWords`, and each `Sql()` batch runs under EF's default 30 s command timeout inside the migration transaction. Fine at today's data size. | Add `CREATE INDEX IX_L ON #Learner([ContentHash], [OwnerUserId])`, and create `IX_VocabularyWords_ContentHash` before step 3–4. Optionally note `--timeout`/`CommandTimeout` in the rollout steps. |
| 4 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.Infrastructure/Services/VocabularyIdRemapSyncService.cs:48-50` | `Task.Delay`/`new PeriodicTimer(...)` sit outside `RunOnceAsync`'s catch. A misconfigured `ContentApi:RemapPollInterval` of `00:00:00` or a negative delay throws `ArgumentOutOfRangeException` from `ExecuteAsync`, and under .NET's default `BackgroundServiceExceptionBehavior.StopHost` that stops Progress. This is config-only, but the plan says the hosted service "never crashes the host". | Validate `ContentApiSettings` at startup (`ValidateDataAnnotations`/`ValidateOnStart`, or a guard that logs and returns when interval ≤ 0). |
| 5 | nit | `WordBuddy/src/Services/Progress/.../RemapVocabularyWordIdsCommandHandler.cs:50-85` | A recall check submitted with a pre-migration (stale) word id **after** that remap was acknowledged creates a stat under the old id. Nothing remaps it later. The window is small (a UI session open across the deploy), and the plan only covers the gap before acknowledgement. | Accept and note it in `docs/features/vocabulary.md`, or have Progress keep the applied pairs and map ids on submit. Not needed for merge. |

Checked with no finding:
- Same-owner precedence (Shared > PendingReview > oldest), plus FIRST_VALUE tiebreak on `Id`.
- Shared/PendingReview rows are never collapsed.
- Different owners' private rows are never merged.
- System ids are preserved, so `vocab-{id}-{locale}.mp3` still resolves, and lesson rows are never merged.
- One link per (owner, FinalId) via GROUP BY. The unique indexes are created after the data and hold by construction.
- Remap rows cover every `FinalId <> Id`, with no chains.
- `Down` rebuild is documented as lossy.
- C#/SQL hash parity: UTF-16LE, `U+001F`/`NCHAR(31)`, space-only trim, upper hex.
- Internal endpoint: `/internal` is not routed by `nginx.conf`, `vite.config.ts` or `k8s/ingress.yaml`. The `InternalService` policy (`wb_service=progress`) cannot be met by Identity tokens. Swagger hides it.
- Token: HS256, 5 min, cached with a 1 min refresh window, never logged.
- Sync never acknowledges a failed remap, is capped at 50 batches, and is idempotent on re-apply. 401/403/404 are not retried. The circuit-breaker sampling satisfies the 2× attempt-timeout rule.
- `ContentApi__BaseUrl` is consistent across appsettings, compose and k8s.
- Clean Architecture/CQRS/Result<T>/XML docs and no cross-service references are followed.
- No lesson-mutating endpoint was added, so `content:lesson:*` needs no new invalidation. The shared-pool cache is still invalidated on moderation.
- UI: `isAuthor` is typed as a union-safe boolean and the Share button is gated on it. Nothing else changed.

## Plan conformance
- Every task in `tasks.md` (rev 2) has matching code and tests in the diff. That covers the domain, configs, migration, repositories, handlers, internal endpoints and policy, the Progress client/token/sync/hosted service, config, the UI, unit and integration tests, and the e2e additions.
- Sam-approved deviations are accepted and not raised as findings: the `Internal` rate-limit policy was dropped, and Progress.UnitTests references Progress.Infrastructure.
- Out of scope: `docs/features/vocabulary.md` (+34) and `docs/features/README.md` were edited in `/code`. The repo convention has the living spec updated by `/test`, so `/test` should review or overwrite these rather than treat them as final.
- Gaps: none in code. E2E runs and the Docker SQL Server integration run are deferred to `/test` (*Verified in /test*). The Content and Progress integration tests ran only on LocalDB.
- Child vs. adult: runtime add/adopt/list paths match today. Finding #2 is the only divergence, and it is in the migration only.
