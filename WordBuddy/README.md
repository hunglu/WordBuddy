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
| Progress | Implemented | Learner progress tracking, streaks, completion history | [src/Services/Progress](./src/Services/Progress/README.md) | `localhost:5083/swagger` |
| Notification | Scaffold only | Push/email notifications for daily phrases and reminders | [src/Services/Notification](./src/Services/Notification/README.md) | `localhost:5084/swagger` |

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

## Aspire (local dev)

One command starts SQL Server, RabbitMQ, the 5 APIs and the Vite UI, with one dashboard for
traces and console logs (ADR [0007](../docs/adr/0007-aspire-local-orchestration.md)). It is a
local dev loop only. Compose and `k8s/` are unchanged.

```
aspire run → AppHost (WordBuddy/aspire) → SQL Server + RabbitMQ (containers)
                                        → identity 5080, content 5081, quiz 5082,
                                          progress 5083, notification 5084 (processes)
                                        → ui 3000 (Vite)
```

**Do not run it together with `make up`.** Both bind 5080-5084, 5672 and 15672. Stop Aspire first
(Ctrl+C); RabbitMQ then frees its ports. The SQL Server container may keep running, but it uses
host port 14330 (compose uses 1433), so it does not block `make up`.

### Prerequisites

1. Docker Desktop running.
2. Aspire CLI installed (see <https://aspire.dev>).
3. `dotnet dev-certs https --trust`
4. Shared packages packed into the local feed (each service restores from it):
   `make pack-shared` (or `dotnet pack` per project, see "Getting started").
5. `npm install` once in `../WordBuddy.UI`.
6. Set the parameters once (placeholders below, use your own values):

```bash
cd aspire/WordBuddy.AppHost
dotnet user-secrets set "Parameters:sql-password" "<strong-sql-sa-password>"
dotnet user-secrets set "Parameters:jwt-secret" "<at-least-32-characters>"
dotnet user-secrets set "Parameters:rabbitmq-password" "<rabbitmq-password>"
# optional, only to override the defaults from appsettings.json
dotnet user-secrets set "Parameters:jwt-issuer" "WordBuddy"
dotnet user-secrets set "Parameters:jwt-expiry-minutes" "60"
dotnet user-secrets set "Parameters:rabbitmq-user" "wordbuddy"
# optional: unset = Autofill AI off
dotnet user-secrets set "Parameters:claude-api-key" "<claude-api-key>"
```

`Properties/launchSettings.json` starts the AppHost in `Development`, so it reads these
user-secrets. If the dashboard asks for a secret, the AppHost did not start in `Development`; do
not save a new value there, because it would no longer match the existing volumes.
Non-secret defaults (`jwt-issuer`, `jwt-expiry-minutes`, `rabbitmq-user`) live in
`appsettings.json`; set them in user-secrets only to override them.

### Run

```bash
cd aspire
aspire run            # or: dotnet run --project WordBuddy.AppHost
```

| What | Where |
|---|---|
| App | <http://localhost:3000> |
| Aspire dashboard | <http://localhost:15180> (login URL with token printed by `aspire run`) |
| Dashboard OTLP (traces in) | `localhost:19180` |
| Dashboard resource service | `localhost:20180` |
| RabbitMQ management | <http://localhost:15672> |
| RabbitMQ AMQP | `localhost:5672` |
| SQL Server | `localhost,14330` (user `sa`, password = `Parameters:sql-password`) |
| Uploaded media | `aspire/.data/media` (gitignored) |

### Good to know

- **Migrations:** each service migrates and seeds its own database on startup in `Development`
  (the same as under compose). No `dotnet ef` step. Fallback if you need it: take the connection
  string from the dashboard and run `dotnet ef database update` for that service.
- **Telemetry:** traces and console logs only. Metrics and structured OTLP logs are not exported
  by `WordBuddy.Shared.Infrastructure`. The AppHost sets `OpenTelemetry__OtlpEndpoint` from the
  endpoint Aspire injects, so traces reach the dashboard even when your local
  `appsettings.Development.json` sets `OpenTelemetry:OtlpEndpoint`. Traces are listed as
  `WordBuddy.Identity` ... `WordBuddy.Notification`; console logs and health stay under the
  `identity` ... `notification` resources.
- **Volumes:** SQL Server and RabbitMQ use their own Aspire volumes, separate from compose. Both
  read their password only on the first start with an empty volume. If you change `sql-password`
  or `rabbitmq-password` later, the logins fail and the APIs never start. Stop Aspire, then delete
  the `sql` container and the `wordbuddy.apphost-*-sql-data` / `-rabbitmq-data` volumes
  (`docker ps -a`, `docker volume ls`) and start again.
- **Tests:** `dotnet test aspire/WordBuddy.Aspire.slnx --filter "Category!=Integration"` runs the
  model and guard tests without Docker. `Category=Integration` starts the real stack on the fixed ports 5080-5084, 3000, 5672 and 15672,
  so stop compose (`make down`) and `aspire run` first. The test SQL Server uses a dynamic host port,
  so the persistent `sql` container of a normal run (port 14330) may keep running.
- The AppHost has no project reference to any service, and no service references the AppHost.

## Running in Docker / local Kubernetes

```bash
cp .env.example .env          # fill in GITHUB_TOKEN (a PAT with read:packages)
make up                       # docker-compose: sqlserver + all 5 APIs + ui

make cluster-up               # kind cluster + ingress-nginx
make k8s-build-load           # build & load all images
cp k8s/secret.yaml.example k8s/secret.yaml   # fill in real values, never commit
make k8s-apply                # namespace, config, secrets, sqlserver, rabbitmq, all services, ingress
make k8s-rabbitmq-ui          # RabbitMQ management UI on http://localhost:15672 (port-forward only, never on the Ingress)
```

See [`CLAUDE.md`](./CLAUDE.md)'s "Running in Docker / local Kubernetes" section for the full
service → image → deployment → ingress-path table and the independence-model rationale.

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
