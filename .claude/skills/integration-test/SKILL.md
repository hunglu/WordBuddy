---
name: integration-test
description: Writes API integration tests for a WordBuddy microservice using WebApplicationFactory against a real SQL Server database (never mocked EF Core). Use when adding or changing an endpoint, a migration that moves data, or an authorization policy (especially child vs adult), or when Sam asks for integration tests in <Service>.IntegrationTests.
---

# Integration tests

Real host, real SQL Server, real JWT — only external services (blob storage, other services) are
faked. Reference implementation: `WordBuddy/src/Services/Content/WordBuddy.Content.IntegrationTests/`.

```text
<Service>ApiFactory (env vars → host → MigrateAsync … EnsureDeletedAsync)
   └─ [CollectionDefinition] <Service>ApiCollection  ← one shared factory per run
        └─ [Collection] <Feature>EndpointsTests → HttpClient + TestJwtTokenFactory token
```

## Building blocks (copy from Content if the service lacks them)

| File | Role |
| --- | --- |
| `<Service>ApiFactory.cs` | `WebApplicationFactory<Program>, IAsyncLifetime`; sets config via **environment variables** in a static ctor (Program reads config before `ConfigureWebHost`); per-run DB name `WordBuddy<Service>Tests_{Guid}`; `<SERVICE>_TEST_CONNECTION_STRING` override for CI |
| `<Service>ApiCollection.cs` | `ICollectionFixture<<Service>ApiFactory>` — all classes share one factory, so they do not race on the same DB |
| `TestJwtTokenFactory.cs` | Issues tokens with `userId`, `ageGroup`, `isAdmin` using the factory's test secret |

## Rules

- `[Collection(<Service>ApiCollection.Name)]` on every test class.
- Name: `{ClassUnderTest}_{Method}_{ExpectedOutcome}`; one `[Fact]`/`[Theory]` per scenario.
- Isolation: each test creates its own users (`Guid.NewGuid()`) and data — never depend on order
  or on another test's rows.
- Assert status code **and** body (`ReadFromJsonAsync` with `JsonStringEnumConverter`), plus the DB
  state for writes (resolve the DbContext from `factory.Services` in a scope).
- Cover per endpoint: happy path, validation failure (`400` ProblemDetails), not found (`404`),
  unauthenticated (`401`), forbidden policy (`403`).
- **Child vs adult:** where behaviour differs, one test per `AgeGroup`. Never log or assert on PII.
- Rate-limited endpoints: assert `429` + `Retry-After` only in a dedicated test.
- No `Thread.Sleep`; FluentAssertions only; `async` all the way.

## Run

```bash
dotnet test WordBuddy/src/Services/<Service>/WordBuddy.<Service>.slnx --filter "FullyQualifiedName~IntegrationTests"
```

Needs a reachable SQL Server (LocalDB, or the Docker one via `<SERVICE>_TEST_CONNECTION_STRING`).
If none is reachable, report the suite as **skipped**, never as passed.
