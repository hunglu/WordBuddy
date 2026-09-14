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
