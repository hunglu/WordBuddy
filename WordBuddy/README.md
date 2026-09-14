# WordBuddy

An English learning web app for children and adults — vocabulary, grammar, daily phrases, and
quizzes, delivered as text, images, audio pronunciations, and video clips.

## Tech stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Web framework | ASP.NET Core Web API (controllers, class-based `Program.cs`) |
| Database | SQL Server |
| ORM | Entity Framework Core |
| Auth | JWT (Auth0 planned) |
| Testing | xUnit, FluentAssertions, Moq |
| Containerization | Docker, Kubernetes (local `kind` cluster) |

## Architecture

5 fully independent microservices — no service references another service or a shared project.
Common code (`Result<T>`/`Error`, cross-cutting infra helpers, message contracts) ships as
versioned NuGet packages, not project references, so any service folder can become its own repo
later with nothing to untangle. See [`CLAUDE.md`](./CLAUDE.md) for the full model and rationale.

| Service | Status | Responsibility | README | Swagger (dev) |
|---|---|---|---|---|
| Identity | Implemented | Registration, login, JWT issuance | [src/Services/Identity](./src/Services/Identity/README.md) | `localhost:5080/swagger` |
| Content | Implemented | Lessons, Vocabulary, Grammar, DailyPhrases, MediaAssets | [src/Services/Content](./src/Services/Content/README.md) | `localhost:5081/swagger` |
| Quiz | Implemented | Quiz definitions, question banks, answer evaluation | [src/Services/Quiz](./src/Services/Quiz/README.md) | `localhost:5082/swagger` |
| Progress | Scaffold only | Learner progress tracking, streaks, completion history | [src/Services/Progress](./src/Services/Progress/README.md) | — |
| Notification | Scaffold only | Push/email notifications for daily phrases and reminders | [src/Services/Notification](./src/Services/Notification/README.md) | — |

## Folder structure

```
WordBuddy/
├── nuget.config
├── local-nuget-feed/        # gitignored
├── CLAUDE.md
├── README.md
└── src/
    ├── Shared/              # WordBuddy.Shared.Kernel / Infrastructure / Contracts (packed, not referenced directly)
    └── Services/
        ├── Identity/
        ├── Content/
        ├── Quiz/
        ├── Progress/
        └── Notification/
```

## Getting started

```bash
# Build, run, and test one service at a time — there's no root solution
dotnet build src/Services/Identity/WordBuddy.Identity.slnx
dotnet run --project src/Services/Identity/WordBuddy.Identity.Api
dotnet test src/Services/Identity/WordBuddy.Identity.slnx
```

The shared libraries are already packed into `local-nuget-feed/` (see `src/Shared/`). If you
change one, repack it before the next `dotnet restore` in a consuming service:
```bash
dotnet pack src/Shared/WordBuddy.Shared.Kernel -o local-nuget-feed
```

## Roadmap

1. **Project Setup** *(this phase)* — independent per-service solution skeletons, shared NuGet
   packages, git housekeeping, docs.
2. **Backend** — domain models, EF Core, repositories, CQRS handlers, controllers, migrations,
   seed data — per service.
3. **Frontend** — React 18 + TypeScript SPA (`WordBuddy.UI`, separate folder).
4. **Infrastructure** — per-service Dockerfiles, local media storage, local Kubernetes (`kind`)
   hosting, GitHub Packages for shared-library and image restore.
5. **Deployment** — GitHub Actions CI/CD: publish shared NuGet packages, build & push per-service
   images, roll out to the local cluster (and later, a real one).
