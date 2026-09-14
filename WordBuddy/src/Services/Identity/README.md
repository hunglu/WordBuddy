# Identity Service

Registration, login, JWT issuance, and refresh token rotation for WordBuddy learner and admin
accounts.

Owns the `User` domain entity and the `AgeGroup` (Child, Adult) enum. Has zero project
references to any other WordBuddy service or shared project — see the root
[`CLAUDE.md`](../../../CLAUDE.md) for the independence model this follows.

## Endpoints

_None yet — added in Phase 2 (expect `POST /api/auth/register`, `POST /api/auth/login`)._

## Running standalone

```bash
dotnet run --project Identity.Api
```

## Configuration

`Identity.Api/appsettings.Development.json` (added in Phase 2) will hold:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyIdentity`)
- `Jwt:Secret` / `Jwt:Issuer` / `Jwt:ExpiryMinutes`

## Projects

| Project | Purpose |
|---|---|
| `Identity.Api` | Controllers, `Program.cs`, composition root |
| `Identity.Application` | Commands/queries, DTOs, validators |
| `Identity.Domain` | `User` entity, `AgeGroup` enum |
| `Identity.Infrastructure` | EF Core `IdentityDbContext`, repositories |
| `Identity.UnitTests` | Handler/domain unit tests (Moq) |
| `Identity.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
