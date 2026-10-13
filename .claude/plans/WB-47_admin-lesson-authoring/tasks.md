# Tasks: WB-47 Admin lesson authoring

Paths below are relative to `WordBuddy/src/Services/Content/` (backend) or `WordBuddy.UI/src/` (frontend) unless noted.

## Backend

- [x] `Lesson` domain mutators (`UpdateDetails`, `Publish`, `Unpublish`, `AttachSense`, `DetachSense`, `ReorderSenses`) + typed `Error`s — methods return `Result`, `SortOrder` stays 0..n-1 — `Domain/Lesson.cs`, `Domain/LessonSense.cs`, `Domain/Sense.cs` (`IsHiddenFromChildrenInLessons`)
- [x] `ILessonRepository` / implementation: `GetByIdForUpdateAsync`, `ListAllAsync`, `Remove`, `SaveChangesAsync` — tracked load includes `LessonSenses` — `Application/Interfaces/ILessonRepository.cs`, `Infrastructure/Repositories/LessonRepository.cs` (also `GetPublishedAsync` gets a `targetAgeGroup` filter)
- [x] Verify / set EF delete behaviour: `Lesson → LessonSense` cascade, `LessonSense → Sense` restrict — migration only if snapshot differs — verified in `LessonConfiguration` (Cascade) and `LessonSenseConfiguration` (NoAction); no change, no migration
- [x] `CreateLessonCommand` creates drafts (`isPublished: false`) — new lesson has `IsPublished = false` — `Application/Features/Lessons/Commands/CreateLesson/CreateLessonCommandHandler.cs`
- [x] `UpdateLesson` command + validator + handler — 404 on unknown id; title/description length rules match `CreateLessonCommandValidator` — `Features/Lessons/Commands/UpdateLesson/*`
- [x] `PublishLesson` / `UnpublishLesson` commands + handlers — idempotent, 404 on unknown id — `Features/Lessons/Commands/PublishLesson/*`, `UnpublishLesson/*`
- [x] `DeleteLesson` command + handler — lesson and its `LessonSenses` removed, senses kept — `Features/Lessons/Commands/DeleteLesson/*`
- [x] `AttachLessonSense` command + validator + handler — only system or `Shared` senses; 409 on duplicate; 400 on non-vocabulary lesson — `Features/Lessons/Commands/AttachLessonSense/*`
- [x] `DetachLessonSense` command + handler — 404 if not attached; remaining order renumbered — `Features/Lessons/Commands/DetachLessonSense/*`
- [x] `ReorderLessonSenses` command + validator + handler — rejects lists that are not exactly the attached set — `Features/Lessons/Commands/ReorderLessonSenses/*`
- [x] `GetAdminLessons` query + handler — returns drafts and published, `isPublished`, `senseCount`, filters type / level / isPublished — `Features/Lessons/Queries/GetAdminLessons/*`, `DTOs/AdminLessonDtos.cs`
- [x] `GetAdminLessonDetail` query + handler — unfiltered senses with `sortOrder`, `visibleToChildren`, `shareStatus` — `Features/Lessons/Queries/GetAdminLessonDetail/*`
- [x] `SearchLessonSenses` admin query (added: open question 5 — the shared pool does not return system senses) — `GET /api/lessons/admin/senses?search=`; `Features/Lessons/Queries/SearchLessonSenses/*`, `IVocabularyWordRepository.SearchAttachableAsync`, `VocabularyWordRepository.cs`
- [x] Child filter in `GetLessonDetailQueryHandler` — child gets 404 for draft or adult-targeted lesson; senses with `VisibleToChildren = false` removed; non-admin gets 404 for drafts — `Features/Lessons/Queries/GetLessonDetail/*` (see report: rule applies to shared-pool senses; unapproved auto-fill words keep the WB-25 placeholder)
- [x] Child filter in `GetLessonsQueryHandler` — child list returns only `TargetAgeGroup = Child` published lessons — `Features/Lessons/Queries/GetLessons/*`
- [x] `LessonsAdminController` (`api/lessons/admin`, `AdminOnly`) with all admin endpoints; remove `POST` from `LessonsController` — non-admin gets 403 on every write — `Api/Controllers/LessonsAdminController.cs`, `Api/Controllers/LessonsController.cs`, `Api/Models/LessonAdminRequests.cs`
- [x] Register new handlers + validators in Application DI — service starts, endpoints resolve — `Application/Extensions/ServiceCollectionExtensions.cs`

## Frontend

- [x] Types in `src/types/index.ts` (`AdminLessonDto`, `AdminLessonDetailDto`, `AdminLessonSenseDto`) — no `any`, enums as unions — `types/index.ts` (+ `AdminSenseSearchResult`, `AdminLessonFilters`, `LessonFormValues`)
- [x] Admin functions in `src/api/lessons.ts` (list, detail, create, update, publish, unpublish, delete, attach, detach, reorder) — match backend routes — `api/lessons.ts` (+ `searchLessonSenses`)
- [x] TanStack Query hooks: `['adminLessons', filters]`, `['adminLesson', id]` + mutations invalidating them and `['lessons']` — `hooks/useAdminLessons.ts` (also invalidates `['lesson']`)
- [x] `AdminLessonsPage` (`/admin/lessons`) — table, filters, status badge, publish toggle, delete confirm, `isError` state — `pages/AdminLessonsPage.tsx`, `components/admin-lessons/ConfirmDialog.tsx`, `components/admin-lessons/LessonStatusBadge.tsx`
- [x] `AdminLessonEditorPage` details form (RHF + Zod), create and edit modes — save creates draft / updates lesson — `pages/AdminLessonEditorPage.tsx`
- [x] Editor senses panel: list, detach, up / down reorder with Framer Motion `layout`, "Hidden from children" badge — `components/admin-lessons/LessonSensesPanel.tsx`
- [x] "Add word" picker using the admin sense search — attaches sense, already-attached senses disabled — `components/admin-lessons/AddWordPicker.tsx`
- [x] Routes + admin-only guard + sidebar link — non-admins cannot reach `/admin/lessons*` — `App.tsx`, `layouts/AppLayout.tsx`, page-level `Navigate` guard

## Tests

- [x] Unit: `Lesson` mutators — attach / detach / reorder / publish paths incl. each error — `UnitTests/Domain/LessonTests.cs`
- [x] Unit: `UpdateLesson`, `Publish`/`Unpublish`, `DeleteLesson` handlers — success + not found — `UnitTests/Features/Lessons/LessonCommandHandlerTests.cs`
- [x] Unit: `AttachLessonSense` handler — system ok, shared ok, private / pending / rejected rejected, duplicate 409 — `UnitTests/Features/Lessons/LessonCommandHandlerTests.cs`
- [x] Unit: `DetachLessonSense`, `ReorderLessonSenses` handlers + validator — renumbering and invalid order — `UnitTests/Features/Lessons/LessonCommandHandlerTests.cs`
- [x] Unit: `GetAdminLessons`, `GetAdminLessonDetail` handlers — drafts included, sense metadata present — `UnitTests/Features/Lessons/LessonQueryHandlerTests.cs`
- [x] Unit: `GetLessonDetail` / `GetLessons` child vs adult — child sees no hidden senses, no drafts, no adult lessons — `UnitTests/Features/Lessons/LessonQueryHandlerTests.cs`
- [x] Integration: `LessonsAdminController` auth — admin 2xx, adult 403, child 403 on every write endpoint — `IntegrationTests/LessonsAdminEndpointsTests.cs`
- [x] Integration: delete lesson keeps `Senses` rows — cascade only on `LessonSenses` — `IntegrationTests/LessonsAdminEndpointsTests.cs` (existing `VocabularyAutofillEndpointsTests` seed now publishes its lesson)
- [x] E2E API (`e2e/api`): admin create → attach → reorder → publish → learner reads in order — written and builds; NOT RUN (needs running services) — `e2e/api/WordBuddy.E2E.Api.Tests/LessonAuthoringTests.cs`
- [x] E2E UI (`e2e/ui`): admin authors lesson in UI; child does not see hidden sense — written; NOT RUN (needs running stack) — `e2e/ui/features/lesson-authoring.feature`, `e2e/ui/steps/lesson-authoring.steps.ts`
