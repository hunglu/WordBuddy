# WordBuddy — Backend

- Backend: .NET 8 Clean Architecture — `./WordBuddy`
- Frontend: React 18 + TypeScript — `./WordBuddy.UI`
- Cross-service end-to-end tests — `./e2e`

@./WordBuddy/CLAUDE.md
@./WordBuddy.UI/CLAUDE.md

## Workflow: idea → plan → code → test

Feature work in this repo follows a 4-stage workflow, each stage its own slash command backed by
a dedicated subagent. State for each proposal lives in its own folder under `.claude/plans/<slug>/`
as plain markdown — that folder *is* the visible tracker (check `status:` in each `proposal.md`).

1. **`/propose <title>`** — Sam captures a business idea. Writes `proposal.md`
   (`status: idea`). No analysis happens here.
2. **`/plan <slug>`** — the `planner` subagent (`.claude/agents/planner.md`) analyzes the
   proposal against this codebase's conventions and writes `plan.md` + `tasks.md`
   (`status: planned`). **This is the approval gate** — implementation does not start
   automatically.
3. **`/code <slug>`** — Sam running this command is the approval. The `coder` subagent
   (`.claude/agents/coder.md`) implements `tasks.md` top to bottom, checking off each task,
   stopping rather than bypassing anything gated by `.claude/settings.json`'s `ask`/`deny`
   lists (`status: implemented` once done).
4. **`/test <slug>`** — the `tester` subagent (`.claude/agents/tester.md`) writes/extends unit
   and integration tests in the touched service's existing test projects, plus E2E coverage in
   `e2e/api` (.NET Playwright, API-level) and `e2e/ui` (TypeScript Playwright + `playwright-bdd`,
   UI-level), and writes `test-report.md` (`status: done` or `needs-fixes`).

`e2e/` sits at the repo root alongside `WordBuddy/` and `WordBuddy.UI/` because it spans both —
it is not owned by, or a dependency of, either.

## Project Overview

WordBuddy is a microservices-based English learning web application built on .NET 8. It serves both children and adults with vocabulary lessons, grammar lessons, daily phrases, and quizzes. Content is delivered as text, images, audio pronunciations, and video clips. This repository contains all backend services; the frontend lives in a separate repository (TBD).

## Domain Concepts

| Concept | Description |
|---|---|
| `User` | Learner or admin account with profile and role |
| `Lesson` | A structured learning unit (vocabulary or grammar) |
| `Vocabulary` | A word entry with definition, examples, and media |
| `Grammar` | A grammar rule with explanations and examples |
| `DailyPhrase` | A phrase surfaced to learners each day |
| `Quiz` | A set of questions testing lesson content |
| `MediaAsset` | Text, image, audio, or video content attached to any domain object |
| `LearnerProgress` | Tracks a user's completion and score across lessons and quizzes |

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| Web framework | ASP.NET Core Web API, use controller, use class-based (no top-level statement) |
| Database | SQL Server |
| ORM | Entity Framework Core 8 |
| Authentication | JWT (access + refresh tokens) via Auth0 |
| Identity Server | Auth0 as 3rd-party |
| Testing | xUnit, FluentAssertions, Moq |
| Logging | Serilog (structured; sinks: Console, File, Seq) |
| Caching | `IDistributedCache` (Redis) |
| Distributed tracing | OpenTelemetry + Jaeger |
| Distributed messaging | MassTransit (Azure Service Bus in production) |
| Health checks | ASP.NET Core Health Checks |
| Rate limiting | ASP.NET Core Rate Limiting middleware |
| Circuit breaker | ASP.NET Core Resilience (`Microsoft.Extensions.Http.Resilience`) |
| Containerization | Docker + Docker Compose |
| CI/CD | GitHub Actions |

## Architecture

Clean Architecture with CQRS. Each microservice is independently deployable and follows the same internal layer structure.

```
Presentation  →  Application  →  Domain  →  Infrastructure
(Controllers)    (Commands/       (Entities,  (EF Core, Redis,
                  Queries,         Value        MassTransit,
                  DTOs,            Objects,     HTTP clients,
                  Validators)      Events)      Serilog)
```

**Layer rules:**

- Domain has zero dependencies on other layers or third-party packages. It may take a
  `<PackageReference>` on `WordBuddy.Shared.Kernel` (`Result<T>`/`Error`/base entity) — that's a
  first-party package built from this same solution family, not an external dependency.
- Application depends only on Domain; it defines interfaces that Infrastructure implements.
- Infrastructure implements all I/O: database, cache, messaging, external APIs, media storage.
- Presentation depends on Application only — never directly on Infrastructure. (The one standard
  exception: a service's `Api` project also references its own `Infrastructure` project, but only
  as the Clean Architecture composition root — to call `AddInfrastructure(...)` in `Program.cs` —
  never to call Infrastructure types from business logic.)
- Use Controller definition (no minimal endpoint), use class-based instead top-level statement.

## Microservices

| Service | Responsibility |
|---|---|
| `IdentityService` | Registration, login, JWT issuance, refresh token rotation |
| `ContentService` | Lessons, Vocabulary, Grammar, DailyPhrases, MediaAssets |
| `QuizService` | Quiz definitions, question banks, answer evaluation |
| `ProgressService` | LearnerProgress tracking, streaks, completion history |
| `NotificationService` | Push/email notifications for daily phrases and reminders |

**Independence model:** no service has a project reference to another service, or to any shared
project — the deliberate goal is that any `src/Services/<Service>/` folder can be lifted into its
own git repository later with nothing to untangle. Consequences:

- Each service has its **own solution** (`src/Services/<Service>/WordBuddy.<Service>.slnx`) containing only
  that service's own projects. There is no root solution that builds all 5 at once.
- Common code (`WordBuddy.Shared.Kernel`, `WordBuddy.Shared.Infrastructure`,
  `WordBuddy.Shared.Contracts`) is never a `ProjectReference` from a service — it's a versioned
  NuGet package every service consumes via `<PackageReference>`, published from two feeds: a
  local file-system feed (`local-nuget-feed/`, gitignored) for fast local `dotnet build`
  iteration, and GitHub Packages (`nuget.pkg.github.com`) for CI and Docker builds (a Docker
  build's context can't see an arbitrary folder on your machine, so it needs a real,
  network-reachable feed).
- Each service folder is self-sufficient: its own `.sln`, `nuget.config`, `Dockerfile`,
  `README.md`, and `UnitTests`/`IntegrationTests` projects.

## Conventions

### General

- SOLID principles throughout.
- `async`/`await` everywhere — no `.Result`, `.Wait()`, or blocking calls.
- No `null` returns from service or repository methods — use `Result<T>` or `Option<T>`.
- Do not use `var` when the type is not immediately obvious from the right-hand side.
- All public API surface is documented with XML doc comments.

### Result Pattern

All application-layer methods return `Result<T>` (or `Result` for void operations).

```csharp
// Success
return Result<LessonDto>.Success(dto);

// Failure
return Result<LessonDto>.Failure(Error.NotFound("Lesson.NotFound", $"Lesson {id} was not found."));
```

`Error` is a typed value object (record with `Code` and `Description`). Never throw exceptions for expected business failures — use `Result`.

### CQRS

- Commands mutate state. Queries read state. Keep them strictly separate.
- Do NOT Use MediatR for dispatching commands and queries, instead using the original in dotnet.
- One handler per command or query.
- Handlers live in the Application layer under `Features/<FeatureName>/`.

```
Features/
  Lessons/
    Commands/
      CreateLesson/
        CreateLessonCommand.cs
        CreateLessonCommandHandler.cs
        CreateLessonCommandValidator.cs
    Queries/
      GetLessonById/
        GetLessonByIdQuery.cs
        GetLessonByIdQueryHandler.cs
  Vocabulary/
    Commands/
      AddVocabulary/
        ...
    Queries/
      GetVocabularyByLesson/
        ...
  Quiz/
    Commands/
      SubmitQuizAnswer/
        ...
    Queries/
      GetQuizById/
        ...
```

### Validation

- FluentValidation for all commands and queries.
- Register a MediatR pipeline behavior that runs validation before the handler.
- Return `Result.Failure` with validation errors — never let `ValidationException` propagate past the pipeline.

### Error Handling

- Global exception middleware catches unhandled exceptions and returns RFC 7807 `ProblemDetails`.
- Expected failures flow through `Result<T>` and are never promoted to exceptions.
- Log unhandled exceptions at `Error` level with full stack trace and correlation ID.

### Logging (Serilog) & Distributed Tracing (OpenTelemetry)

**Both are implemented once, in `WordBuddy.Shared.Infrastructure`'s `Observability` folder —
never re-implement Serilog/OpenTelemetry setup per service.** Every service's `Program.cs` wires
them with exactly two calls:

```csharp
builder.Host.ConfigureWordBuddySerilog(ServiceName);
// ...
builder.Services.AddWordBuddyOpenTelemetry(ServiceName, builder.Configuration);
```

- `ServiceName` is a `private const string` in `Program.cs`, always in the form
  **`WordBuddy.<Service>`** (e.g. `"WordBuddy.Identity"`, `"WordBuddy.Content"`) — never the bare
  service name — so it reads unambiguously in logs/traces/Jaeger regardless of what else is
  running.
- Structured logging only — never use string interpolation in log messages; use message
  templates. Log via `ILogger<T>` (`Microsoft.Extensions.Logging`) everywhere — never a Serilog
  type directly; Serilog is only the backend, wired once at the host level.
- Every log entry is enriched with `CorrelationId` (the current OpenTelemetry trace id — ties
  together every log line from one request, including Serilog's own request-logging summary),
  `ServiceName`, and `Environment` — this enrichment lives in `CorrelationIdEnricher` +
  `ConfigureWordBuddySerilog`, not per-service code.
- Log levels: `Debug`, `Information`, `Warning`, `Error`, `Fatal`. Use `Verbose` only for local
  trace-level diagnostics.
- Never log sensitive data: passwords, tokens, PII, or child user data.
- Sinks/levels are configuration, not code: a `Serilog` section in `appsettings.json`
  (Console-only, `Warning` default — production-safe) and `appsettings.Development.json`
  (Console + rolling file, `Information` default). Copy an existing service's sections rather
  than inventing new ones.
- `AddWordBuddyOpenTelemetry` instruments inbound HTTP, outbound `HttpClient`, and EF Core
  automatically, exporting via OTLP. Default endpoint is Jaeger's local OTLP/gRPC port
  (`http://localhost:4317`), overridable via `OpenTelemetry:OtlpEndpoint` in config or the
  standard `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable for other environments — harmless
  if nothing is listening there (the host still starts cleanly).
- `traceparent` propagation across service boundaries comes for free from the ASP.NET Core +
  `HttpClient` instrumentation above — don't hand-roll header forwarding.

### Caching

- Cache reads in query handlers using `IDistributedCache`.
- Cache key pattern: `{ServiceName}:{EntityName}:{Id}` — e.g., `content:lesson:42`.
- Always set an absolute expiry. Do not use sliding expiry by default.
- Invalidate via cache-aside on command success (delete the key after a successful write).
- Cache high-read, low-mutation content aggressively: `Vocabulary`, `Grammar`, `DailyPhrase`.

### Health Checks

Every service exposes:

- `GET /health` — liveness (returns 200 if the process is up).
- `GET /health/ready` — readiness (checks DB, Redis, downstream service dependencies).

### Rate Limiting

- Applied globally via ASP.NET Core Rate Limiting middleware.
- Per-endpoint policies defined in `RateLimitingConfiguration`.
- Return `429 Too Many Requests` with a `Retry-After` header.
- Apply stricter limits on quiz submission and auth endpoints to prevent abuse.

### Authentication & Authorization

- JWT bearer tokens issued by `IdentityService` (backed by Auth0).
- Refresh token rotation persisted in the database.
- Use policy-based authorization — avoid hardcoded role strings in `[Authorize]` attributes.
- Child accounts (`AgeGroup = Child`) are subject to additional content restrictions enforced via authorization policies.

### Media Assets

- `MediaAsset` records store the asset type (`Text`, `Image`, `Audio`, `Video`), a storage URL, and metadata.
- Actual binary files are stored in blob storage (Azure Blob or local volume in development); the database holds only references.
- Audio pronunciation files follow the naming convention: `vocab-{vocabularyId}-{locale}.mp3`.
- Video clips are linked per `Lesson` or `Vocabulary` entry and streamed from the CDN URL stored in `MediaAsset`.

### Distributed Messaging

- MassTransit with in-memory transport for local development; Azure Service Bus in staging and production.
- Published domain events (e.g., `QuizCompletedEvent`, `LessonCompletedEvent`) drive `ProgressService` updates asynchronously.
- Consumers are idempotent — processing the same message twice must not corrupt state.
- Message contracts live in `WordBuddy.Shared.Contracts`.

### Testing

- Unit tests for domain logic and application handlers; mock infrastructure interfaces with Moq.
- Integration tests for API endpoints and database using `WebApplicationFactory` and a real SQL Server (Docker).
- Never mock EF Core — integration tests must hit a real database.
- Test class naming: `{ClassUnderTest}_{Method}_{ExpectedOutcome}`.
- One `[Fact]` or `[Theory]` per logical scenario.
- Cover both child-targeted and adult-targeted content paths where behavior differs.

### Docker / Docker Compose

- Each service has its own multi-stage `Dockerfile` **inside its own service folder**, with the
  build context scoped to that folder only (not the repo root) — consistent with the
  independence model. Restoring `WordBuddy.Shared.*` inside a Docker build therefore uses the
  GitHub Packages feed (not `local-nuget-feed/`, which a Docker build context can't see),
  authenticated via a BuildKit secret — never a plaintext token in an `ARG`/`ENV`/layer.
- `docker-compose.yml` at repo root orchestrates all services + SQL Server + Redis + Jaeger for local development.
- Never hardcode connection strings — use environment variables or `appsettings.{Environment}.json` (excluded from git).

## Environment Configuration

| File | Purpose |
|---|---|
| `appsettings.json` | Non-secret defaults |
| `appsettings.Development.json` | Local overrides (not in git) |
| `appsettings.Production.json` | Production overrides (not in git) |
| Environment variables | Secrets in CI/CD and containers |

Secrets (connection strings, JWT signing keys, Auth0 credentials, storage account keys) are never committed. Use `dotnet user-secrets` locally.

## Build & Run

There is no root solution (see the independence model above) — restore/build/test one service
at a time, against its own `.sln`:

```bash
# Restore, build, and test one service
dotnet restore src/Services/<ServiceName>/WordBuddy.<ServiceName>.slnx
dotnet build src/Services/<ServiceName>/WordBuddy.<ServiceName>.slnx
dotnet test src/Services/<ServiceName>/WordBuddy.<ServiceName>.slnx

# Run a specific service
dotnet run --project src/Services/<ServiceName>/WordBuddy.<ServiceName>.Api

# Run all services with Docker Compose (local dev inner loop)
docker compose up --build

# Repack a shared library after changing it, for local restore
dotnet pack src/Shared/WordBuddy.Shared.Kernel -o local-nuget-feed
```

## Project Structure

Per the independence model above: no root solution, no cross-service or service→shared project
references. `k8s/`, `docker-compose.yml`, and the Makefile live at the root because they're
orchestration/ops concerns that legitimately span all 5 services at *runtime* — that's not the
same thing as source-level coupling.

```
WordBuddy/
├── nuget.config                # nuget.org + local-nuget-feed + GitHub Packages (root convenience)
├── local-nuget-feed/           # gitignored — dotnet pack output for fast local restore
├── .env.example
├── docker-compose.yml          # local dev inner loop: sqlserver + all 5 APIs + ui
├── kind-config.yaml            # local Kubernetes (kind) cluster config
├── Makefile                    # pack-shared, publish-shared, cluster-up, k8s-apply, ...
├── .github/
│   └── workflows/
├── .dockerignore
├── .gitignore
├── CLAUDE.md
├── README.md
│
├── k8s/                        # orchestration only — not owned by any one service
│   ├── namespace.yaml / configmap.yaml / secret.yaml.example
│   ├── sqlserver-statefulset.yaml / sqlserver-service.yaml / sqlserver-init-job.yaml
│   ├── <service>-deployment.yaml / <service>-service.yaml   # one pair per service
│   ├── ui-deployment.yaml / ui-service.yaml
│   └── ingress.yaml
│
└── src/
    ├── Shared/
    │   ├── WordBuddy.Shared.sln              # convenience solution for developing the 3 libs together
    │   ├── WordBuddy.Shared.Kernel/           # Result<T>, Error, base entity — packed as a NuGet package
    │   ├── WordBuddy.Shared.Infrastructure/   # Serilog/OpenTelemetry/health-check/rate-limit helpers — packed
    │   └── WordBuddy.Shared.Contracts/        # MassTransit message contracts — packed
    │
    └── Services/
        ├── Identity/
        │   ├── WordBuddy.Identity.slnx                  # only this service's own projects
        │   ├── nuget.config                  # local feed + GitHub Packages + nuget.org (self-sufficient)
        │   ├── Dockerfile                     # build context = this folder only
        │   ├── README.md
        │   ├── WordBuddy.Identity.Api/
        │   ├── WordBuddy.Identity.Application/
        │   ├── WordBuddy.Identity.Domain/
        │   ├── WordBuddy.Identity.Infrastructure/
        │   ├── WordBuddy.Identity.UnitTests/
        │   └── WordBuddy.Identity.IntegrationTests/
        ├── Content/         # same shape — WordBuddy.Content.Application owns Lessons, Vocabulary, Grammar, DailyPhrase, MediaAsset
        ├── Quiz/            # same shape
        ├── Progress/        # same shape — WordBuddy.Progress.Application owns LearnerProgress
        └── Notification/    # same shape
```

## CI/CD (GitHub Actions)

Pipelines live in `.github/workflows/`. Standard pipeline per service:

1. Restore & build.
2. Run unit tests.
3. Run integration tests (spin up SQL Server + Redis containers).
4. Build Docker image.
5. Push to container registry on merge to `main`.

---

# WordBuddy.UI — CLAUDE.md

## Project overview

Frontend for **WordBuddy**, an English learning app for children and adults.
React 18 + TypeScript SPA served by Vite, talking to the WordBuddy ASP.NET Core API.

Backend lives at `https://github.com/hunglu/WordBuddy` (separate repo).

---

## Stack

| Layer | Library | Version |
| --- | --- | --- |
| Build | Vite | 8.x |
| UI | React 18 | 19.x |
| Routing | React Router | 7.x (v6-compatible API) |
| Server state | TanStack Query | 5.x |
| Client state | Zustand | 5.x |
| Styling | TailwindCSS | 4.x |
| Animation | Framer Motion | 12.x |
| Forms | React Hook Form + Zod | 7.x / 4.x |
| HTTP | Axios | 1.x |

---

## Folder structure

```
src/
├── api/            Axios functions per backend controller
│   ├── client.ts   Shared Axios instance — JWT interceptor, 401 handler
│   ├── auth.ts     loginUser, registerUser
│   ├── lessons.ts  getLessons, getLessonDetail
│   └── progress.ts getProgress, markLessonComplete
│
├── components/     Reusable UI components
│   ├── lessons/    Lesson-specific sub-components
│   │   ├── VocabularyList.tsx
│   │   ├── GrammarRuleList.tsx
│   │   └── DailyPhraseList.tsx
│   └── ProtectedRoute.tsx
│
├── layouts/        Route-level layout wrappers
│   ├── PublicLayout.tsx   Centered card — login / register
│   └── AppLayout.tsx      Sidebar + top bar — all protected pages
│
├── pages/          One file per route
│   ├── LoginPage.tsx
│   ├── RegisterPage.tsx
│   ├── DashboardPage.tsx
│   ├── LessonsPage.tsx
│   ├── LessonDetailPage.tsx
│   └── ProgressPage.tsx
│
├── store/          Zustand stores (client state only)
│   ├── authStore.ts   token, user, isAuthenticated, login(), logout()
│   └── lessonStore.ts currentLesson, lessons[]
│
├── types/          TypeScript interfaces mirroring backend DTOs
│   └── index.ts    User, Lesson, LessonDetail, VocabularyItem,
│                   GrammarRule, DailyPhrase, MediaAsset,
│                   LearnerProgress, AgeGroup, Level, LessonType
│
├── hooks/          Custom React hooks (project-specific)
├── utils/          Pure helpers and constants
└── assets/         Static images, icons, fonts
```

---

## Environment variables

| Variable | Purpose | Example |
|---|---|---|
| `VITE_API_URL` | Base URL of the WordBuddy backend | `https://localhost:5001` |

Set in `.env.development` for local dev. The Vite proxy (`/api → VITE_API_URL`) is configured in `vite.config.ts` so CORS is not an issue during development.

---

## Conventions

### Server state — TanStack Query only

Never use raw `useEffect` + `fetch`/`axios` for API calls.
Every API call must go through a `useQuery` or `useMutation` hook.
Query keys follow the pattern `['resource', ...params]`, e.g. `['lessons', typeFilter, levelFilter]`.

### Typing — no `any`

All API response types are defined in `src/types/index.ts` and used as generics on `apiClient.get<T>()` / `apiClient.post<T>()`.
Never cast to `any`. Use `unknown` + type guards when the shape is uncertain.

### Client state — Zustand only

Zustand stores hold only client-owned state: auth session, UI preferences.
Server data (lesson lists, progress) stays in TanStack Query's cache — do not copy it into Zustand.

### Animations — Framer Motion only

All transitions and entrance animations use Framer Motion.
No CSS `transition`/`animation` for anything interactive.
No inline `style` for animation values.
`Variants` objects must be typed with `import { type Variants } from 'framer-motion'` to satisfy TS strict mode.

### Styling — TailwindCSS only

No inline `style={{}}` props. No CSS Modules. No plain CSS files beyond `src/index.css` (which only contains `@import "tailwindcss"`).

### Auth interceptor

`src/api/client.ts` attaches `Authorization: Bearer <token>` to every request by reading from `useAuthStore.getState()` (outside React — store instance, not hook).
On 401 → calls `logout()` and hard-redirects to `/login`.

### Protected routes

`src/components/ProtectedRoute.tsx` decodes the JWT `exp` claim.
Expired token → `logout()` + redirect, not just missing token check.

## Local Overrides

If ./docs/local-overrides.md does not exist, tell the user explicitly:
"No local-overrides.md found — create one from docs/local-overrides.md.example if you need personal config."
@./docs/local-overrides.md

After any runs, write output down to plans folder
