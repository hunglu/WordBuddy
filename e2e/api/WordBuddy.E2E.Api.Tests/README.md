# WordBuddy.E2E.Api.Tests

Blackbox API end-to-end tests for the WordBuddy backend, using `Microsoft.Playwright`'s
`IAPIRequestContext` for HTTP-level testing. This project has **no project reference to any
service** — it only ever talks HTTP, consistent with the microservices independence model (see
`../../../CLAUDE.md`).

## Prerequisites

- The backend running locally, e.g. `make up` (or `docker compose up --build`) from `WordBuddy/`.
- One-time only: restore this project once (`dotnet restore`) so the `Microsoft.Playwright`
  driver is available. API-level testing does not need browser binaries, so
  `playwright install` is **not** required for this project (only for `../ui`).

## Running

```bash
dotnet test e2e/api/WordBuddy.E2E.Api.Tests
```

## Pointing at a different environment

Base URLs default to the docker-compose local-dev ports (see `appsettings.json`). Override any
of them with an environment variable, e.g. to target a local Kubernetes (`kind`) deployment:

```bash
Services__Identity=http://localhost:30080 dotnet test e2e/api/WordBuddy.E2E.Api.Tests
```

## Adding a new test

One class per service/feature. Follow `HealthCheckTests.cs`: use the shared
`ApiRequestContextFixture` (via `[Collection(ApiRequestContextCollection.Name)]`) to get an
`IAPIRequestContext` scoped to the service's base URL from `ServiceUrls`, and dispose it when
done. Test naming follows the repo convention: `{ClassUnderTest}_{Method}_{ExpectedOutcome}`.
