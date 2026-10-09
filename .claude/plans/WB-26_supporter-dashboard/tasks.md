# Tasks: WB-26_Supporter dashboard

## Backend (Progress)

- [x] Add `VocabularySessionIssue` domain entity (insert-only, `Create` factory) — ids, time and `PlannedCount` only; no setters. — Domain/VocabularySessionIssue.cs
- [x] Add `DashboardOptions` (`DefaultDays`, `MinSample`, `FastWrongMs`, `HintRateFlag`) bound from config — defaults 30 / 10 / 1500 / 0.3. — Domain/DashboardOptions.cs, Api/Extensions/ServiceCollectionExtensions.cs (AddDashboard), Api/Program.cs
- [x] Add `DashboardCalculator` domain service: activity (streak, active days/week, heatmap, % daily goal) — local-day grouping via client offset; dates only. — Application/Features/Dashboard/DashboardCalculator.cs (in Application, not Domain, so it returns the DTOs directly)
- [x] `DashboardCalculator`: words (added per day/week by `Self`/`Supporter`, per status, reviews per day) — matches the definitions table in plan.md. — DashboardCalculator.cs
- [x] `DashboardCalculator`: true retention overall + per skill — `IsDue && AttemptNo == 1`; `null` below `MinSample`. — DashboardCalculator.cs
- [x] `DashboardCalculator`: struggle (leeches, weakest skill, top 10 slowest by median) and gaming signals (fast wrong, hint rate, unfinished sessions) — matches plan.md. — DashboardCalculator.cs
- [x] Add `IVocabularySessionIssueRepository` + EF implementation + `VocabularySessionIssueConfiguration` — index `(UserId, IssuedAtUtc)`. — Application/Interfaces/IVocabularySessionIssueRepository.cs, Infrastructure/Repositories/VocabularySessionIssueRepository.cs, Infrastructure/Persistence/Configurations/VocabularySessionIssueConfiguration.cs, ProgressDbContext.cs
- [x] `GetVocabularySessionQueryHandler` inserts one `VocabularySessionIssue` per issued session — planned count = due + new items. — Application/Features/VocabularySrs/Queries/GetVocabularySession/GetVocabularySessionQueryHandler.cs (empty sessions are not recorded)
- [x] Add `IDashboardReadRepository` + EF implementation — `AsNoTracking`, projected columns, window-bounded by `OccurredAtUtc`. — Application/Interfaces/IDashboardReadRepository.cs, Infrastructure/Repositories/DashboardReadRepository.cs
- [x] Add migration `AddDashboard` — creates `VocabularySessionIssues` only. — Infrastructure/Persistence/Migrations/20261009095206_AddDashboard*.cs + snapshot, docs/database-diagram/progress.md + README.md (generated only, not applied)
- [x] Add DTOs `LearnerDashboardDto` (+ `Activity`, `Words`, `Retention`, `Struggle`, `GamingSignals`) — XML docs; `DateOnly`, no time-of-day field anywhere. — Application/DTOs/LearnerDashboardDtos.cs
- [x] Add `Features/Dashboard/Queries/GetLearnerDashboard/` query, validator, handler per develop-webapi skill — `Days` in {7, 30, 90}; returns `Result<LearnerDashboardDto>`. — Application/Features/Dashboard/Queries/GetLearnerDashboard/* (query, validator, handler), Application/Extensions/ServiceCollectionExtensions.cs
- [x] Cache in handler with key `progress:dashboard:{learnerId}:{days}:{offsetMinutes}`, absolute expiry 5 min — second call within TTL hits cache. — GetLearnerDashboardQueryHandler.cs, Application csproj (Microsoft.Extensions.Caching.Abstractions), Infrastructure/Extensions/ServiceCollectionExtensions.cs (AddDistributedMemoryCache)
- [x] Add `DashboardController` (`api/progress/dashboard`): `GET me` (`ChildHasSupporter`) and `GET learners/{learnerId:guid}` (`CanSupportLearner`) — failures via `ToProblemResult`. — Api/Controllers/DashboardController.cs
- [x] Add rate-limit policy for dashboard endpoints in `RateLimitingConfiguration` — 429 + `Retry-After`. — Api/Extensions/RateLimitingConfiguration.cs (policy `dashboard`, 30/min/user)
- [x] Register new services/options in Application/Infrastructure `ServiceCollectionExtensions` — app starts, endpoints resolve. — Application, Infrastructure and Api ServiceCollectionExtensions.cs
- [x] Logging: ids and counts only — no metric values, timings or child data in any log line. — handler logs LearnerId, Days, ReviewCount, ErrorCode only
- [x] Update `WordBuddy/src/Services/Progress/README.md` with the new endpoints. — Progress/README.md

## Frontend (WordBuddy.UI)

- [x] Add dashboard interfaces and `DashboardRange` union to `src/types/index.ts` — no `any`, no `enum`. — WordBuddy.UI/src/types/index.ts
- [x] Add `getMyDashboard` / `getLearnerDashboard` to `src/api/progress.ts` — sends `X-Client-CurrentDateTime`. — WordBuddy.UI/src/api/progress.ts
- [x] Add `useMyDashboard` / `useLearnerDashboard` hooks — keys `['dashboard','me',days]` / `['dashboard',learnerId,days]`. — WordBuddy.UI/src/hooks/useDashboard.ts
- [x] Add `components/dashboard/` widgets (`RetentionCard`, `StreakCard`, `ActivityHeatmap`, `DailyGoalBar`, `WordsAddedChart`, `WordStatusBreakdown`, `StruggleList`, `GamingSignals`) — theme tokens only, CSS Modules for the heatmap, no new chart library. — WordBuddy.UI/src/components/dashboard/* (8 widgets + ActivityHeatmap.module.css)
- [x] Resolve sense ids to word text via existing vocabulary query — placeholder text when Content fails. — StruggleList.tsx (uses useSenses; "Word not available" placeholder)
- [x] Add `LearnerDashboardPage` with range selector, routes `/dashboard/me` and `/dashboard/learners/:learnerId` in `App.tsx` — Framer Motion entrance; `isError` and 403 show a message, no crash. — WordBuddy.UI/src/pages/LearnerDashboardPage.tsx, App.tsx
- [x] Add "My dashboard" link on `ProgressPage` and "View dashboard" per active learner on `SupportLinksPage` — links open the right route. — ProgressPage.tsx, SupportLinksPage.tsx
- [x] Neutral wording for gaming signals (child and adult) — no blame text. — GamingSignals.tsx ("Quick wrong answers", "Answer patterns")

## Tests

- [x] Unit `DashboardCalculatorTests`: streak (today/yesterday/broken), active days per week, heatmap dates across a non-UTC offset — one fact per case. — UnitTests/Features/Dashboard/DashboardCalculatorTests.cs
- [x] Unit `DashboardCalculatorTests`: retention counts only first attempts of due reviews; per skill; `null` below `MinSample`. — DashboardCalculatorTests.cs
- [x] Unit `DashboardCalculatorTests`: words added by `Self` vs `Supporter`, per status, reviews per day. — DashboardCalculatorTests.cs
- [x] Unit `DashboardCalculatorTests`: leeches, weakest skill, slowest words (median, ≥ 3 answers), fast-wrong, hint rate, unfinished sessions (today's open session excluded), % daily goal capped at 100. — DashboardCalculatorTests.cs
- [x] Unit `DashboardCalculatorTests`: rebuild — same input rows give identical output. — DashboardCalculatorTests.cs
- [x] Unit `VocabularySessionIssueTests` — factory sets fields; no mutators. — UnitTests/Domain/VocabularySessionIssueTests.cs
- [x] Unit `GetLearnerDashboardQueryHandlerTests`: cache hit, cache miss + set with absolute expiry, repository failure → `Result.Failure`. — UnitTests/Features/Dashboard/GetLearnerDashboardQueryHandlerTests.cs
- [x] Unit `GetLearnerDashboardQueryValidatorTests` — invalid `Days` rejected. — UnitTests/Features/Dashboard/GetLearnerDashboardQueryValidatorTests.cs
- [x] Unit `GetVocabularySessionQueryHandlerTests` — writes one session issue with correct planned count. — UnitTests/Features/VocabularySrs/GetVocabularySessionQueryHandlerTests.cs
- [x] Integration `DashboardEndpointsTests` `GET me`: adult 200; child with supporter 200; child without supporter 403 `Learner.SupporterRequired`. — IntegrationTests/DashboardEndpointsTests.cs
- [x] Integration `DashboardEndpointsTests` `GET learners/{id}`: active supporter of adult 200; active supporter of child 200; revoked link 403; no link 403; learner calling own id without link 403. — IntegrationTests/DashboardEndpointsTests.cs
- [x] Integration: response JSON has no time-of-day fields — assert on serialized body. — IntegrationTests/DashboardEndpointsTests.cs
- [x] Integration: logs captured during a child dashboard call contain no metric values or timings. — IntegrationTests/DashboardEndpointsTests.cs (captures the handler logger)
- [x] API E2E (`e2e/api`, tester): supporter links → learner reviews → supporter dashboard shows retention. — moved to `/test` (Sam, 2026-10-09)
- [x] UI E2E (`e2e/ui`, tester): learner opens own dashboard; supporter opens learner dashboard from `/support`; error state renders when Progress is down. — moved to `/test` (Sam, 2026-10-09)
