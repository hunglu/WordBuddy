# WordBuddy Backend Solution

This file is scoped to the `WordBuddy/` solution folder specifically — for the full product
overview, domain concepts, and conventions shared with the frontend, see `../CLAUDE.md` at the
repo root. This doc covers only what's specific to *this* folder: the independence model, the
concrete structure actually on disk, and how to build/run it.

## Stack

ASP.NET Core **.NET 10** (SDK `10.0.401`), EF Core, SQL Server, JWT (Auth0 planned, self-issued
JWT for now — see Phase 2), xUnit + FluentAssertions + Moq.

> Note: the root `../CLAUDE.md` stack table still says ".NET 8" — that's stale as of this phase.
> Trust this file and the actual `.csproj`/`global.json` files over that line.

## The independence model — and why

WordBuddy is **5 fully independent microservices**: `Identity`, `Content`, `Quiz`, `Progress`,
`Notification`. Unlike a typical shared-solution monorepo, **no service has a project reference
to another service or to a shared project**. The goal: any `src/Services/<Service>/` folder can
be lifted into its own git repository later with nothing to untangle.

Consequences, all already in place:

- **No root `.sln`.** Each service has its own `WordBuddy.<Service>.slnx` (this repo's .NET 10 SDK
  generates the newer XML solution format) containing only that service's own 6 projects
  (`Api`, `Application`, `Domain`, `Infrastructure`, `UnitTests`, `IntegrationTests`).
- **Common code ships as NuGet packages, not project references.** `WordBuddy.Shared.Kernel`
  (`Result<T>`/`Error`), `WordBuddy.Shared.Infrastructure` (cross-cutting Serilog/health-check/
  rate-limit helpers), and `WordBuddy.Shared.Contracts` (MassTransit message contracts) live in
  `src/Shared/` with their own convenience `WordBuddy.Shared.slnx`, and are packed +
  version-pinned (`1.0.0` to start). Two feeds serve the same packages:
  - `local-nuget-feed/` (gitignored, at the solution root) — fast, no-auth restore for local
    `dotnet build`/`dotnet run`. Repack after changing a shared library:
    `dotnet pack src/Shared/WordBuddy.Shared.Kernel -o local-nuget-feed`
  - GitHub Packages (`nuget.pkg.github.com`) — what CI and Docker builds restore from (Phase 4+),
    since a Docker build's context can't see an arbitrary folder on your machine.
- Every service folder is self-sufficient: its own `nuget.config` (pointing at both feeds plus
  nuget.org), its own `README.md`, its own test projects.

## Folder structure (as actually created)

```
WordBuddy/
├── nuget.config                       # nuget.org + local-nuget-feed + github-wordbuddy
├── local-nuget-feed/                  # gitignored — dotnet pack output
├── CLAUDE.md                          # this file
├── README.md
│
└── src/
    ├── Shared/
    │   ├── WordBuddy.Shared.slnx
    │   ├── WordBuddy.Shared.Kernel/
    │   ├── WordBuddy.Shared.Infrastructure/
    │   └── WordBuddy.Shared.Contracts/
    │
    └── Services/
        ├── Identity/
        │   ├── WordBuddy.Identity.slnx
        │   ├── nuget.config
        │   ├── README.md
        │   ├── WordBuddy.Identity.Api/
        │   ├── WordBuddy.Identity.Application/
        │   ├── WordBuddy.Identity.Domain/
        │   ├── WordBuddy.Identity.Infrastructure/
        │   ├── WordBuddy.Identity.UnitTests/
        │   └── WordBuddy.Identity.IntegrationTests/
        ├── Content/         # same shape
        ├── Quiz/            # same shape
        ├── Progress/        # same shape
        └── Notification/    # same shape
```

`Dockerfile` per service, `k8s/`, `docker-compose.yml`, `kind-config.yaml`, and the `Makefile`
are added in Phase 4 — not part of this phase's scaffold.

## Domain entities — service ownership

| Service | Owns | Enums |
|---|---|---|
| Identity | `User` | `AgeGroup` (Child, Adult) |
| Content | `Lesson`, `Vocabulary`, `Grammar`, `DailyPhrase`, `MediaAsset` | `Level`, `LessonType`, `MediaAssetType` |
| Quiz | `Quiz`, `QuizQuestion` | `QuizQuestionType` |
| Progress | `LearnerProgress` | — |
| Notification | (none yet — scaffold only until Phase 2 reaches it) | — |

Naming matches the root `../CLAUDE.md` domain concepts table.

## Conventions

- Clean architecture per service: `Api → Application → Domain`, `Infrastructure → Application`.
  `Api` also references its own `Infrastructure` — the standard composition-root exception, to
  call `AddInfrastructure(...)` in `Program.cs`, never to call Infrastructure types from
  business logic.
- Hand-rolled CQRS: `ICommandHandler`/`IQueryHandler` interfaces defined once per service under
  `Application/Abstractions/` — **no MediatR** (see the `develop-webapi` skill for the exact
  shapes and handler template).
- `Result<T>`/`Error` (from `WordBuddy.Shared.Kernel`) instead of exceptions/`null` for expected
  failures.
- `async`/`await` everywhere, SOLID, no `var` when the type isn't obvious from the right-hand
  side, XML doc comments on public APIs.
- Controllers, not minimal APIs; class-based `Program.cs`, not top-level statements (wired in
  Phase 2).
- `ILogger<T>` (Microsoft.Extensions.Logging, backed by Serilog) — structured message templates,
  never string interpolation.
- Serilog + OpenTelemetry setup is never per-service code — every `Program.cs` calls
  `builder.Host.ConfigureWordBuddySerilog(ServiceName)` and
  `builder.Services.AddWordBuddyOpenTelemetry(ServiceName, builder.Configuration)` from
  `WordBuddy.Shared.Infrastructure`'s `Observability` folder. `ServiceName` is always
  `"WordBuddy.<Service>"` (e.g. `"WordBuddy.Identity"`), never the bare service name. See root
  `../CLAUDE.md`'s Logging/Tracing section for the full contract (enrichment, sinks, OTLP
  endpoint) — don't re-derive it here or in a new service.

## Build & run

There's no root solution — build/run/test one service at a time:

```bash
dotnet build src/Services/<Service>/WordBuddy.<Service>.slnx
dotnet run --project src/Services/<Service>/WordBuddy.<Service>.Api
dotnet test src/Services/<Service>/WordBuddy.<Service>.slnx
```

Repack a shared library after changing it (only needed before another service's next restore
picks it up, or before a Docker build):
```bash
dotnet pack src/Shared/WordBuddy.Shared.Kernel -o local-nuget-feed
```

## Running in Docker / local Kubernetes (Phase 4)

Hosting target is **local Kubernetes via `kind`**, not a cloud VM — a real cloud cluster
(managed EKS vs. self-managed k3s) is a deliberately deferred decision, not yet made.
`docker-compose.yml` stays as a fast inner-loop option for day-to-day dev; the `k8s/` manifests
are the "real" deployment shape you test locally before any cloud target is chosen.

**Why Docker builds need GitHub Packages, not `local-nuget-feed/`:** per the independence model
above, each service's Dockerfile build context is that service's own folder only
(`src/Services/<Service>/`) — consistent with "this folder could be its own repo." A Docker
build therefore can't see `local-nuget-feed/` (it's outside the context) and instead restores
`WordBuddy.Shared.*` from GitHub Packages (`nuget.pkg.github.com`), authenticated via a BuildKit
`--secret` (never a plaintext `ARG`/`ENV` — that would bake the token into image history). Run
`make publish-shared` once after changing a shared library and before rebuilding any image.

| Service | Image | Deployment / Service (k8s) | Ingress path prefix |
|---|---|---|---|
| Identity | `wordbuddy-identity:dev` | `identity-api` | `/api/auth` |
| Content | `wordbuddy-content:dev` | `content-api` | `/api/lessons`, `/api/media` |
| Quiz | `wordbuddy-quiz:dev` | `quiz-api` | `/api/quiz` |
| Progress | `wordbuddy-progress:dev` | `progress-api` | `/api/progress` |
| Notification | `wordbuddy-notification:dev` | `notification-api` | *(no route yet — scaffold only)* |
| WordBuddy.UI | `wordbuddy-ui:dev` | `wordbuddy-ui` | `/` (catch-all) |

```bash
# docker-compose (fast inner loop, needs .env from .env.example + GITHUB_TOKEN)
make up

# local Kubernetes
make cluster-up                    # kind cluster + ingress-nginx
make k8s-build-load                # build all 5 images + ui with the BuildKit secret, kind load
make k8s-apply                     # namespace, configmap, secret.yaml (copy from secret.yaml.example first), sqlserver, all deployments/services, ingress
kubectl -n wordbuddy get pods -w
make k8s-migrate SERVICE=identity  # repeat per service with entities (skip notification)
```

**Known gap, left for the user to verify with their own credentials:** the GitHub Packages
restore path inside a Docker build (`RUN --mount=type=secret,id=github_token ...`) has not been
live-tested end-to-end with a real PAT in this environment — only that it *fails cleanly and
correctly* when the secret is omitted (`docker build` without `--secret` errors at the
`dotnet nuget add source` step with "Both UserName and Password must be specified", proving the
token is never silently skipped or baked into a layer). The first real build needs the user's
own PAT (`read:packages`, and `write:packages` if also running `make publish-shared`) exported as
`GITHUB_TOKEN`.
