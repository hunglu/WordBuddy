<!-- Backend-specific rules only. Project-wide context (domain, workflow, docs map) is in
     ../CLAUDE.md — don't repeat it here. Stable rules only; no run logs or progress notes. -->

# WordBuddy backend

ASP.NET Core (.NET version: see `global.json` / `*.csproj`), EF Core, SQL Server, JWT
(self-issued for now, Auth0 planned), xUnit + FluentAssertions + Moq.

## Independence model

Five fully independent microservices — `Identity`, `Content`, `Quiz`, `Progress`,
`Notification`. **No service has a project reference to another service or to shared code**, so
any `src/Services/<Service>/` can later become its own repo (ADR `../docs/adr/0001`).

- No root solution. Each service has its own `WordBuddy.<Service>.slnx` with its own 6 projects
  (`Api`, `Application`, `Domain`, `Infrastructure`, `UnitTests`, `IntegrationTests`), its own
  `nuget.config`, `Dockerfile` (build context = that folder only), and `README.md`.
- Shared code (`WordBuddy.Shared.Kernel` — `Result<T>`/`Error`/base entity;
  `WordBuddy.Shared.Infrastructure` — Serilog/OTel/health/rate-limit helpers;
  `WordBuddy.Shared.Contracts` — MassTransit messages) lives in `src/Shared/` and ships as
  versioned NuGet packages from two feeds:
  - `local-nuget-feed/` (gitignored) for local builds —
    `dotnet pack src/Shared/WordBuddy.Shared.Kernel -o local-nuget-feed`
  - GitHub Packages for CI and Docker (`make publish-shared`), authenticated with a BuildKit
    `--secret`, never a plaintext `ARG`/`ENV`.

```
WordBuddy/
├── nuget.config  global.json  Makefile  docker-compose.yml  kind-config.yaml
├── k8s/                     # runtime orchestration for all services
├── local-nuget-feed/        # gitignored
└── src/
    ├── Shared/              # WordBuddy.Shared.slnx + Kernel / Infrastructure / Contracts
    └── Services/<Service>/  # slnx, nuget.config, Dockerfile, README, 6 projects
```

| Service | Owns | Enums |
| --- | --- | --- |
| Identity | `User` | `AgeGroup` |
| Content | `Lesson`, `Vocabulary`, `Grammar`, `DailyPhrase`, `MediaAsset` | `Level`, `LessonType`, `MediaAssetType` |
| Quiz | `Quiz`, `QuizQuestion` | `QuizQuestionType` |
| Progress | `LearnerProgress` | — |
| Notification | — (scaffold, no public endpoints yet) | — |

## Architecture — Clean Architecture + CQRS

`Api → Application → Domain`, `Infrastructure → Application`.

- Domain: no dependencies except the `WordBuddy.Shared.Kernel` package.
- Application: depends only on Domain; defines the interfaces Infrastructure implements.
- Infrastructure: all I/O — EF Core, Redis, MassTransit, HTTP clients, blob storage.
- Api: controllers only (no minimal APIs), class-based `Program.cs` (no top-level statements).
  References its own Infrastructure only as the composition root (`AddInfrastructure(...)`).
- Hand-rolled CQRS — **no MediatR**. `ICommandHandler`/`IQueryHandler` live in
  `Application/Abstractions/`; one handler per command/query, under
  `Features/<Feature>/{Commands|Queries}/<UseCase>/`. Follow the `develop-webapi` skill for the
  exact shapes.

## Coding conventions

- `async`/`await` everywhere — no `.Result`/`.Wait()`.
- No `var` when the type isn't obvious from the right-hand side. XML doc comments on public APIs.
- Application methods return `Result<T>`/`Result`; failures are typed `Error`s
  (`Error.NotFound("Lesson.NotFound", "...")`). Never throw or return `null` for expected
  failures.
- Validation: FluentValidation for every command/query, run by a validation decorator around the
  handler; failures come back as `Result.Failure`, never a propagated `ValidationException`.
- Unhandled exceptions: global middleware → RFC 7807 `ProblemDetails`, logged at `Error` with
  stack trace and correlation id.

## Cross-cutting (always via `WordBuddy.Shared.Infrastructure`)

- **Logging/tracing:** every `Program.cs` calls exactly
  `builder.Host.ConfigureWordBuddySerilog(ServiceName)` and
  `builder.Services.AddWordBuddyOpenTelemetry(ServiceName, builder.Configuration)`, where
  `private const string ServiceName = "WordBuddy.<Service>"`. Never re-implement per service.
  Log through `ILogger<T>` with message templates (no interpolation). Sinks/levels are config:
  copy an existing service's `Serilog` sections. OTLP endpoint defaults to `http://localhost:4317`,
  override with `OpenTelemetry:OtlpEndpoint` / `OTEL_EXPORTER_OTLP_ENDPOINT`.
- **Caching:** `IDistributedCache` in query handlers; key `{service}:{entity}:{id}`
  (`content:lesson:42`); always absolute expiry; delete the key after a successful command.
- **Health:** `GET /health` (liveness) and `GET /health/ready` (DB, Redis, downstream).
- **Rate limiting:** global middleware, per-endpoint policies in `RateLimitingConfiguration`,
  `429` + `Retry-After`; stricter on auth and quiz submission.
- **Auth:** JWT bearer, refresh-token rotation persisted in DB, policy-based `[Authorize]` (no
  hardcoded role strings); child restrictions as policies.
- **Messaging:** MassTransit (in-memory locally, Azure Service Bus later); contracts in
  `WordBuddy.Shared.Contracts`; consumers idempotent.
- **Media:** `MediaAsset` holds type + storage URL + metadata; binaries in blob storage (local
  volume in dev). Audio: `vocab-{vocabularyId}-{locale}.mp3`.

## Testing

- Unit: domain + handlers, Moq for infrastructure interfaces.
- Integration: `WebApplicationFactory` + real SQL Server (Docker). Never mock EF Core.
- Naming `{ClassUnderTest}_{Method}_{ExpectedOutcome}`; one `[Fact]`/`[Theory]` per scenario;
  cover child and adult paths where they differ.
- Cross-service API E2E lives in `../e2e/api` (tester agent, `/test`).

## Configuration

`appsettings.json` = non-secret defaults (Serilog Console, `Warning`).
`appsettings.Development.json` / `appsettings.Production.json` are not in git. Secrets via
`dotnet user-secrets` locally and environment variables in containers/CI.

## Build & run

```bash
dotnet build src/Services/<Service>/WordBuddy.<Service>.slnx
dotnet test  src/Services/<Service>/WordBuddy.<Service>.slnx
dotnet run --project src/Services/<Service>/WordBuddy.<Service>.Api
```

## Docker / local Kubernetes (kind)

`docker-compose.yml` is the fast inner loop; `k8s/` on kind is the real deployment shape (cloud
target not chosen yet). Rebuild images only after `make publish-shared` if a shared lib changed.

| Service | Image | k8s Deployment | Ingress prefix |
| --- | --- | --- | --- |
| Identity | `wordbuddy-identity:dev` | `identity-api` | `/api/auth` |
| Content | `wordbuddy-content:dev` | `content-api` | `/api/lessons`, `/api/media` |
| Quiz | `wordbuddy-quiz:dev` | `quiz-api` | `/api/quiz` |
| Progress | `wordbuddy-progress:dev` | `progress-api` | `/api/progress` |
| Notification | `wordbuddy-notification:dev` | `notification-api` | — |
| UI | `wordbuddy-ui:dev` | `wordbuddy-ui` | `/` |

```bash
make up                              # docker compose (needs .env + GITHUB_TOKEN)
make cluster-up && make k8s-build-load && make k8s-apply
make k8s-migrate SERVICE=identity    # per service with entities
```

Known gap: the GitHub Packages restore inside `docker build` has not been verified end-to-end
with a real PAT (`read:packages`; `write:packages` for `make publish-shared`).
