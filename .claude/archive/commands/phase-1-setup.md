---
title: Phase 1 — Project Setup
description: Initialize WordBuddy microservices — each service fully independent, ready to split into its own repo later
status: draft
---

## Context

WordBuddy is 5 independent microservices (`Identity`, `Content`, `Quiz`, `Progress`,
`Notification`). Unlike a typical shared-solution monorepo, **no service references another
service or a shared project** — the explicit goal is that any `src/Services/<Service>/` folder
can be lifted out into its own git repo later with zero untangling. Consequences of that goal:

- **No root `.sln`.** Each service has its own `WordBuddy.<Service>.slnx` containing only that service's
  own projects. There's no single "build everything" solution — build/test each service folder
  independently (the Makefile loops over them for convenience).
- **Common code (`Result<T>`/`Error`, cross-cutting Serilog/health-check/rate-limit helpers,
  MassTransit message contracts) is NOT a project reference.** It's packed as versioned NuGet
  packages (`WordBuddy.Shared.Kernel`, `WordBuddy.Shared.Infrastructure`,
  `WordBuddy.Shared.Contracts`) that every service consumes via `<PackageReference>`, same as any
  third-party package. Two feeds:
  - A **local file-system feed** (`local-nuget-feed/`, gitignored) for fast local `dotnet build`
    iteration — no network, no auth.
  - **GitHub Packages** (`nuget.pkg.github.com`) as the feed CI and Docker builds restore from —
    a Docker build's context can't see an arbitrary folder on your machine, so it needs a real,
    network-reachable feed. Both feeds serve the exact same packages; the local one is just a
    faster loop for you.
- Stack: **.NET 10**, SQL Server, React 18, Docker, Kubernetes (local `kind` cluster for now).

## Target folder structure

```
WordBuddy/
├── nuget.config                # nuget.org + local-nuget-feed + GitHub Packages
├── local-nuget-feed/           # gitignored — dotnet pack output for fast local restore
├── .gitignore
├── CLAUDE.md
├── README.md
│
└── src/
    ├── Shared/
    │   ├── WordBuddy.Shared.sln
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
(`Dockerfile` per service, `k8s/`, `docker-compose.yml`, `kind-config.yaml`, and `Makefile` are
added in Phase 4 — not part of this phase's scaffold.)

## Requirements

- [ ] 5 service folders under `src/Services/`, each with its own `.sln`, own `Api`/
      `Application`/`Domain`/`Infrastructure` projects, own `UnitTests`/`IntegrationTests`, own
      `nuget.config`, own `Dockerfile`, own `README.md` — zero project references to any other
      service or to the shared libraries
- [ ] `src/Shared/` holds the 3 shared library projects (own convenience `.sln`, not required by
      any service to build)
- [ ] Local NuGet feed (`local-nuget-feed/`) + root `nuget.config` wiring it in, plus a GitHub
      Packages source for CI/Docker
- [ ] `.gitignore` covers .NET + Node + VS Code + `.env` + `local-nuget-feed/` (repo already
      exists with history and a remote — no `git init` needed)
- [ ] Root `CLAUDE.md` written with stack, conventions, and the actual project structure
- [ ] Root `README.md` plus one `README.md` per service
- [ ] First commit made for this skeleton

## Implementation Plan

### Step 1 — Shared libraries (packages, not project references)

```powershell
dotnet new sln -n WordBuddy.Shared -o src/Shared
dotnet new classlib -n WordBuddy.Shared.Kernel -o src/Shared/WordBuddy.Shared.Kernel -f net10.0
dotnet new classlib -n WordBuddy.Shared.Infrastructure -o src/Shared/WordBuddy.Shared.Infrastructure -f net10.0
dotnet new classlib -n WordBuddy.Shared.Contracts -o src/Shared/WordBuddy.Shared.Contracts -f net10.0
dotnet sln src/Shared/WordBuddy.Shared.sln add src/Shared/WordBuddy.Shared.Kernel src/Shared/WordBuddy.Shared.Infrastructure src/Shared/WordBuddy.Shared.Contracts
```
`WordBuddy.Shared.Infrastructure` may reference `WordBuddy.Shared.Kernel` via `ProjectReference`
(they're packed and versioned together, released as a pair) — that's fine, it's internal to the
shared-libs solution, not a service.

Each shared `.csproj` gets a `<PackageId>`/`<Version>` (start `1.0.0`). Add a `local-nuget-feed/`
folder at the solution root (gitignored) and pack into it:
```powershell
dotnet pack src/Shared/WordBuddy.Shared.Kernel -o local-nuget-feed
dotnet pack src/Shared/WordBuddy.Shared.Infrastructure -o local-nuget-feed
dotnet pack src/Shared/WordBuddy.Shared.Contracts -o local-nuget-feed
```

Root `nuget.config` (committed):
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="local-wordbuddy" value="./local-nuget-feed" />
    <add key="github-wordbuddy" value="https://nuget.pkg.github.com/hunglu/index.json" />
  </packageSources>
</configuration>
```
Publishing to GitHub Packages (`dotnet nuget push ... --source github-wordbuddy`, using a PAT
with `write:packages`) happens whenever shared code changes — document as a `make publish-shared`
target; not required for every local iteration, only before a Docker build needs the new version.

### Step 2 — Per-service projects

Repeat for each of `Identity`, `Content`, `Quiz`, `Progress`, `Notification`:
```powershell
dotnet new sln -n <Service> -o src/Services/<Service>
dotnet new webapi -n WordBuddy.<Service>.Api -o src/Services/<Service>/WordBuddy.<Service>.Api -controllers -f net10.0
dotnet new classlib -n WordBuddy.<Service>.Application -o src/Services/<Service>/WordBuddy.<Service>.Application -f net10.0
dotnet new classlib -n WordBuddy.<Service>.Domain -o src/Services/<Service>/WordBuddy.<Service>.Domain -f net10.0
dotnet new classlib -n WordBuddy.<Service>.Infrastructure -o src/Services/<Service>/WordBuddy.<Service>.Infrastructure -f net10.0
dotnet new xunit -n WordBuddy.<Service>.UnitTests -o src/Services/<Service>/WordBuddy.<Service>.UnitTests -f net10.0
dotnet new xunit -n WordBuddy.<Service>.IntegrationTests -o src/Services/<Service>/WordBuddy.<Service>.IntegrationTests -f net10.0

dotnet sln src/Services/<Service>/WordBuddy.<Service>.slnx add (every .csproj just created for this service)

dotnet add src/Services/<Service>/WordBuddy.<Service>.Application reference src/Services/<Service>/WordBuddy.<Service>.Domain
dotnet add src/Services/<Service>/WordBuddy.<Service>.Infrastructure reference src/Services/<Service>/WordBuddy.<Service>.Application
dotnet add src/Services/<Service>/WordBuddy.<Service>.Api reference src/Services/<Service>/WordBuddy.<Service>.Application src/Services/<Service>/WordBuddy.<Service>.Infrastructure
dotnet add src/Services/<Service>/WordBuddy.<Service>.UnitTests reference src/Services/<Service>/WordBuddy.<Service>.Application src/Services/<Service>/WordBuddy.<Service>.Domain
dotnet add src/Services/<Service>/WordBuddy.<Service>.IntegrationTests reference src/Services/<Service>/WordBuddy.<Service>.Api

dotnet add src/Services/<Service>/WordBuddy.<Service>.UnitTests package FluentAssertions
dotnet add src/Services/<Service>/WordBuddy.<Service>.UnitTests package Moq
dotnet add src/Services/<Service>/WordBuddy.<Service>.IntegrationTests package FluentAssertions
```
These references are the *only* project-level wiring inside a service — all within its own
folder. `Domain` has no reference at all yet (it will take a `PackageReference` on
`WordBuddy.Shared.Kernel` once that's needed in Phase 2, not a `ProjectReference`).

Each service folder gets its own `nuget.config` (same 3 sources as the root one — a copy, not a
reference to the root file, since this folder must be self-sufficient once extracted to its own
repo):
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="local-wordbuddy" value="../../../local-nuget-feed" />
    <add key="github-wordbuddy" value="https://nuget.pkg.github.com/hunglu/index.json" />
  </packageSources>
</configuration>
```
(The relative `local-wordbuddy` path only resolves while the service lives inside this monorepo —
that's expected; once split out, the service falls back to the GitHub Packages source, which
still works since it's a real remote feed, not a relative path.)

`-controllers` satisfies CLAUDE.md's "use Controller, no minimal endpoints" — the top-level→
class-based `Program.cs` refactor is Phase 2 scope, left as the template generates it here.

### Step 3 — `.gitignore`

`D:\...\sources` is already a git repo (remote `origin` → `github.com/hunglu/WordBuddy.git`,
existing history) — skip `git init`. Extend the existing `.gitignore` (currently only excludes
`.env`):
```gitignore
## .NET
bin/
obj/
*.user
*.suo
.vs/

## NuGet
local-nuget-feed/

## Node (WordBuddy.UI)
node_modules/
dist/
build/
.env.local

## VS Code
.vscode/*
!.vscode/extensions.json
```

### Step 4 — Root `CLAUDE.md`

Content must include:

- Project name/purpose/features (from root CLAUDE.md, condensed)
- Stack: ASP.NET Core .NET 10, EF Core, SQL Server, JWT, xUnit + FluentAssertions + Moq
- **The independence model**: no service references another service or a shared project; common
  code ships as versioned NuGet packages from `local-nuget-feed/` (dev) and GitHub Packages
  (CI/Docker) — explain *why* (future repo split) so it isn't mistaken for an oversight later
- The actual per-service + shared-lib folder structure created in Steps 1–2
- Domain entities table (matching root `sources/CLAUDE.md`'s naming): `User`, `Lesson`,
  `Vocabulary`, `Grammar`, `DailyPhrase`, `Quiz`, `MediaAsset`, `LearnerProgress` — and which
  service owns each
- How to build/test one service (`dotnet build src/Services/<Service>/WordBuddy.<Service>.slnx`) and how to
  re-pack + republish a shared library after changing it

### Step 5 — Root `README.md` + per-service `README.md`

Root `README.md`: project overview, tech stack badges, the folder structure from Steps 1–2,
getting-started steps, a roadmap of all 5 phases, and a link to each service's own README.

Each `src/Services/<Service>/README.md`: that service's responsibility, its endpoints (filled in
by Phase 2), how to run it standalone (`dotnet run --project WordBuddy.<Service>.Api`), its own env vars/
connection string, and a note that it has zero dependencies on sibling services.

### Step 6 — First commit

```bash
git add WordBuddy/ .gitignore
git commit -m "chore: phase 1 — independent per-service microservices skeleton"
```

## Verification

- `dotnet build src/Services/Identity/WordBuddy.Identity.slnx` (and the same for the other 4) →
  `Build succeeded`, 0 errors, using only that service's own projects
- `dotnet pack`+`dotnet nuget push` round-trip: pack a shared lib into `local-nuget-feed/`,
  confirm a service's `dotnet restore` picks it up via `<PackageReference>`
- No `.csproj` under `src/Services/**` contains a `<ProjectReference>` pointing outside its own
  service folder
- Root `CLAUDE.md`/`README.md` and every service's `README.md` exist
- `git log --oneline -1` shows the new commit
