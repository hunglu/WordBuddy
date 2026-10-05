# Tasks: WB-21 Messaging RabbitMQ foundation

## Shared

- [x] Add `LearnerWordAdded` / `LearnerWordRemoved` records in `WordBuddy.Shared.Contracts` (no MassTransit ref), bump to `1.1.0` — ids + timestamps only, XML docs state the no-text rule. — `src/Shared/WordBuddy.Shared.Contracts/Vocabulary/LearnerWordAdded.cs`, `LearnerWordRemoved.cs`, `.csproj`
- [x] Add `AddWordBuddyMessaging<TDbContext>` + `AddWordBuddyMessagingEntities` in `WordBuddy.Shared.Infrastructure`, bump to `1.1.0` — RabbitMQ default, `InMemory` via config, EF outbox/inbox, retry, OTel source, health check tagged `ready`. — `Shared.Infrastructure/Messaging/MessagingExtensions.cs`, `Observability/OpenTelemetryExtensions.cs`, `.csproj` (MassTransit 8.5.11, Apache-2.0; RabbitMQ delayed redelivery not used, needs a broker plugin)
- [x] Pack both packages to `local-nuget-feed` — Content and Progress restore `1.1.0`. — `local-nuget-feed/` (gitignored); GitHub Packages publish not done (ask-gated)

## Backend — Content

- [x] Reference new Shared versions; call `AddWordBuddyMessaging<ContentDbContext>` in `AddInfrastructure` — service starts with RabbitMQ config. — Content `Api.csproj`, `Infrastructure.csproj`, `Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- [x] Map outbox entities in `ContentDbContext` + migration `AddMessagingOutbox` — migration builds; no apply without Sam. — `ContentDbContext.cs`, `Migrations/20261005095815_AddMessagingOutbox*`, snapshot, `docs/database-diagram/content.md`
- [x] Add `LearnerWordEventCollector` + `SaveChangesAsync` override publishing for `LearnerWord` Added/Deleted — one `OutboxMessage` per link change, same transaction. — `Infrastructure/Messaging/LearnerWordEventCollector.cs`, `ContentDbContext.cs` (failed save drops its outbox rows)
- [x] Audit `VocabularyWordRepository` (`DeleteIfOrphanedAsync`, `UnlinkAsync`, hand-over) for untracked/cascade deletes of `LearnerWord` — every removal path emits `LearnerWordRemoved`; system-owner links never publish. — no code change: no `ExecuteDelete`/raw SQL on `LearnerWords`; `DeleteIfOrphanedAsync` deletes a sense only with zero links, so the DB cascade never removes a link; hand-over adds no system-owner link
- [x] Add RabbitMQ config keys to `appsettings.json` (no password) — secrets only via env/user-secrets. — Content + Progress `appsettings.json` (`Messaging` section, no password)

## Backend — Progress

- [x] Add `LearnerWordMembership` domain entity with `RecordAdded` / `RecordRemoved` (ignore older events) — returns `Result`. — `Progress.Domain/LearnerWordMembership.cs` (`CreateAdded`, `CreateRemoved`, `RecordAdded`, `RecordRemoved` → `Result<bool>`)
- [x] Add EF configuration (unique `UserId`,`SenseId`), inbox/outbox entities, migration `AddLearnerWordMembership` — migration builds. — `LearnerWordMembershipConfiguration.cs`, `ProgressDbContext.cs`, `Migrations/20261005100809_AddLearnerWordMembership*`, snapshot, `docs/database-diagram/progress.md`
- [x] Add `ILearnerWordMembershipRepository` + implementation (upsert) — no duplicate row on concurrent insert (unique-violation handled). — `Application/Interfaces/ILearnerWordMembershipRepository.cs`, `Infrastructure/Repositories/LearnerWordMembershipRepository.cs`
- [x] Add `RecordLearnerWordAdded` / `RecordLearnerWordRemoved` commands, validators, handlers per `develop-webapi` — return `Result`, log ids only. — `Application/Features/LearnerWords/Commands/RecordLearnerWord{Added,Removed}/*` (6 files), `Application/Extensions/ServiceCollectionExtensions.cs`
- [x] Add `LearnerWordAddedConsumer` / `LearnerWordRemovedConsumer`, register via `AddWordBuddyMessaging<ProgressDbContext>` — consumers call the handlers; failure result → throw for retry. — `Infrastructure/Messaging/LearnerWord{Added,Removed}Consumer.cs`, `Infrastructure/Extensions/ServiceCollectionExtensions.cs`

## Ops

- [x] Add `rabbitmq` service to `docker-compose.yml` with healthcheck; Content/Progress depend on it — `make up` starts all green. — `docker-compose.yml`, `.env.example`; static check `docker compose config` OK; `make up` not run (ask-gated)
- [x] Add RabbitMQ manifests + Secret template to `k8s/`, env vars on `content-api` / `progress-api` — `make k8s-apply` brings broker up. — `k8s/rabbitmq-deployment.yaml`, `k8s/rabbitmq-service.yaml`, `k8s/secret.yaml.example`, `k8s/configmap.yaml`, `k8s/{content,progress}-deployment.yaml`, `Makefile` (`k8s-apply`, `k8s-rabbitmq-ui`), `README.md`; `make k8s-apply` not run (ask-gated, cluster down)
- [x] Update Content/Progress `README.md` (broker config, migrations) — commands listed. — `src/Services/Content/README.md`, `src/Services/Progress/README.md`

## Docs

- [x] Remove `/internal/vocabulary-remaps` from `docs/architecture.md`; add RabbitMQ to the diagram — no stale endpoint. — `docs/architecture.md`
- [x] Update `docs/product/roadmap.md` with the vocabulary themes (WB-21..WB-29) — themes listed with order. — `docs/product/roadmap.md`

## Tests

- [x] Unit: `LearnerWordEventCollector_Collect_*` — Added → `LearnerWordAdded`, Deleted → `LearnerWordRemoved`, system-owner link → none. — `Content.IntegrationTests/Messaging/LearnerWordEventCollectorTests.cs` (pure unit tests; UnitTests has no Infrastructure ref)
- [x] Unit: `LearnerWordMembership_RecordAdded/RecordRemoved_*` — older event ignored, re-add after remove works. — `Progress.UnitTests/Domain/LearnerWordMembershipTests.cs`
- [x] Unit: Progress command handlers (validation failure, upsert, remove unknown = success). — `Progress.UnitTests/Features/LearnerWords/RecordLearnerWordCommandHandlerTests.cs`
- [x] Unit: `AddWordBuddyMessaging` registers in-memory transport when configured. — `Content.IntegrationTests/Messaging/MessagingRegistrationTests.cs`
- [x] Integration (Content, `InMemory` + MassTransit test harness): add, adopt, delete, confirmed hand-over, orphan delete → expected event published; child and adult user give same payload with no text fields. — `Content.IntegrationTests/Messaging/LearnerWordEventPublishingTests.cs`, `ContentApiFactory.cs`
- [x] Integration (Content): rollback of `SaveChanges` → no outbox row. — `LearnerWordEventPublishingTests.SaveChangesRolledBack_*`
- [x] Integration (Progress, test harness): consume `LearnerWordAdded` → row; same `MessageId` twice → one row; `Removed` → inactive. — `Progress.IntegrationTests/Messaging/LearnerWordConsumerTests.cs`, `ProgressApiFactory.cs`
- [x] Integration: `/health/ready` includes broker check in Content and Progress. — `Content.IntegrationTests/Messaging/BrokerHealthCheckTests.cs`, `LearnerWordConsumerTests` (Progress)
- E2E (`e2e/api`) — **handed to the tester (`/test`)**, not a coder task: add word in Content → membership in Progress in compose and kind (via DB or test hook).
