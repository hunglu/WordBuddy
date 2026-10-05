# Architecture

WordBuddy is a React SPA in front of 5 independent .NET microservices, each with its own database
(ADR `adr/0001-independent-microservices.md`).

## 1. System overview

```mermaid
flowchart LR
    B[Browser] -->|HTTPS| UI["wordbuddy-ui<br/>nginx :3000 / ingress /"]
    UI -->|/api/auth| ID[Identity :5080]
    UI -->|/api/lessons · /api/media · /api/vocabulary| CT[Content :5081]
    UI -->|/api/quiz| QZ[Quiz :5082]
    UI -->|/api/progress| PR[Progress :5083]
    NT[Notification :5084<br/>scaffold, no public routes]
    CT -.->|"LearnerWordAdded / LearnerWordRemoved<br/>(EF outbox)"| MQ[(RabbitMQ<br/>AMQP 5672)]
    MQ -.->|"consumers + EF inbox"| PR
    ID --> DB1[(Identity DB)]
    CT --> DB2[(Content DB)]
    CT --> FS[(media volume)]
    QZ --> DB3[(Quiz DB)]
    PR --> DB4[(Progress DB)]
```

- **Routing by path prefix.** Vite proxy (dev), the UI's nginx (Docker) and the k8s ingress apply the same prefixes; the frontend code never changes.
- **No cross-database joins or FKs.** Services exchange ids only. There are no service-to-service HTTP calls today.
- **Messaging (WB-21).** Async events via MassTransit on RabbitMQ (`AddWordBuddyMessaging` in `WordBuddy.Shared.Infrastructure`). Publishers write to an EF Core outbox in the same transaction; consumers dedupe with an EF Core inbox. Payloads carry ids and timestamps only. The RabbitMQ management UI is internal: `127.0.0.1` in compose, `kubectl port-forward` in kind, never on the ingress.
- **Auth.** Identity issues JWTs; every service validates them.
- **One SQL Server instance** hosts all databases locally (`sqlserver` in compose); each service owns its schema and migrations.

## 2. Inside one service — Clean Architecture + CQRS

```mermaid
flowchart TB
    API["Api<br/>controllers, Program.cs (composition root)"] --> APP["Application<br/>command / query handlers, validators, interfaces"]
    APP --> DOM["Domain<br/>entities, Result&lt;T&gt;, Error"]
    INF["Infrastructure<br/>EF Core, HTTP clients, storage, MassTransit"] --> APP
    API -->|AddInfrastructure| INF
```

Dependency inversion: controllers call `ICommandHandler` / `IQueryHandler`; Infrastructure implements Application's interfaces; Domain depends only on `WordBuddy.Shared.Kernel`.

## 3. Request pipeline

```mermaid
flowchart TD
    REQ([HTTP request]) --> LOG[Serilog request logging]
    LOG --> JWT{JWT valid?}
    JWT -- No --> R401([401])
    JWT -- Yes --> AZ{Policy allowed?}
    AZ -- No --> R403([403])
    AZ -- Yes --> CTR[Controller → Command / Query]
    CTR --> VAL{FluentValidation}
    VAL -- invalid --> R400([400])
    VAL -- valid --> H[Handler → Repository → SQL]
    H --> RES{Result}
    RES -- Success --> OK([200 / 201 / 204])
    RES -- NotFound --> R404([404])
    RES -- Conflict --> R409([409])
    H -. unhandled exception .-> GEX[Global middleware → ProblemDetails]
    GEX -.-> R500([500])
```

- Expected failures travel as `Result.Failure` — handlers never throw for them.
- Unhandled exceptions are logged at `Error` with the correlation id and returned as RFC 7807 `ProblemDetails`.
