# Tasks: Production deployment: single host (Docker Compose) and Kubernetes

## Docs (first â€” fixes the decisions)

- [x] Write `docs/adr/0010-production-deployment-topology.md` (status `proposed`) â€” covers the same images, Compose + Caddy, the Kustomize overlay, `migrate` mode (one-shot / initContainer), Postgres default, and backups. — docs/adr/0010-production-deployment-topology.md

## Backend â€” shared

- [x] `WordBuddy.Shared.Infrastructure`: add `AddWordBuddyForwardedHeaders(IConfiguration)` + `UseWordBuddyForwardedHeaders()` (reads `ForwardedHeaders:KnownNetworks`; empty = no-op), version `1.4.0` â€” `dotnet pack` to `local-nuget-feed` succeeds. — Shared.Infrastructure/ForwardedHeadersExtensions.cs, .csproj 1.4.0 (packed to local-nuget-feed)

## Backend â€” services

- [x] Identity: `Api/Extensions/MigrationRunner.cs` + `migrate` arg in `Program.Main` (migrate, then exit 0/1; no web server) â€” `dotnet WordBuddy.Identity.Api.dll migrate` exits 0 on an empty Postgres DB. — Identity.Api MigrationRunner.cs, Program.cs; verified on SQL Server by MigrationAndBootstrapTests (Postgres not run: no local server)
- [x] Identity: `AdminBootstrapOptions` (`Bootstrap:Admin` Email/Password/DisplayName) + `Infrastructure/Seeding/AdminBootstrapper.cs`, called from `migrate` â€” creates one Adult admin if none exists, skips if one exists, exits 1 if not configured; never logs email or password. — Identity.Infrastructure Seeding/AdminBootstrapOptions.cs, AdminBootstrapper.cs
- [x] Content: `MigrationRunner.cs` + `migrate` arg â€” migrates only, no sample lessons. — Content.Api MigrationRunner.cs, Program.cs
- [x] Quiz: `MigrationRunner.cs` + `migrate` arg â€” migrates only, no sample quiz. — Quiz.Api MigrationRunner.cs, Program.cs
- [x] Progress: `MigrationRunner.cs` + `migrate` arg â€” migrates only. — Progress.Api MigrationRunner.cs, Program.cs
- [x] Identity, Content, Quiz, Progress: bump `WordBuddy.Shared.Infrastructure` to `1.4.0` and call the forwarded-headers helper first in the pipeline â€” each `.slnx` builds; Development behaviour unchanged. — 4 Api Program.cs + WebApplicationExtensions.cs, 7 csproj (Quiz moved 1.0.2 to 1.4.0)
- [x] `WordBuddy.UI/nginx.conf`: add `X-Forwarded-For` / `X-Forwarded-Proto` to every `/api/*` location â€” `make up` still serves the app. — WordBuddy.UI/nginx.conf (not run through make up: docker is ask-gated)

## Deployment â€” single host (`WordBuddy/deploy/single-host/`)

- [ ] `docker-compose.yml`: caddy, wordbuddy-ui, 5 APIs, 4 `*-migrate` one-shots, postgres, postgres-init, rabbitmq (`4-alpine`), backup; images only (no `build:`), pinned tags, `mem_limit`, `restart: unless-stopped`; only caddy publishes 80/443 â€” `docker compose config` passes. — WRITTEN, NOT RUN: `docker compose config` is ask-gated. Needs Sam
- [ ] `postgres/init.sh`: idempotent creation of 4 DBs + 4 owner roles from env â€” a second run changes nothing. — WRITTEN, `sh -n` ok; idempotency needs a running Postgres
- [ ] `Caddyfile`: `{$SITE_ADDRESS}` â†’ `wordbuddy-ui:80`, HSTS, 50 MB body limit; `localhost` uses `tls internal` â€” `caddy validate` passes. — WRITTEN; `caddy validate` needs docker
- [x] `.env.example`: every variable (version, registry, site address, DB role passwords, JWT, RabbitMQ, admin bootstrap, optional Claude key, `ForwardedHeaders__KnownNetworks__0`) with placeholder values only â€” no real secret. — deploy/single-host/.env.example
- [ ] `scripts/health.sh`: curl one-shot on the Compose network checks `/health/ready` on 5 APIs â€” prints one line per API and exits non-zero on any failure. — WRITTEN, `sh -n` ok; needs the running stack
- [ ] `backup/backup.sh` + crontab (02:00, retain 14 days, `pg_dump -Fc` per DB + media tar, files `0600`) â€” a manual run creates `./backups/<date>/` with 5 files. — WRITTEN, `sh -n` ok; needs the running stack
- [ ] `scripts/restore.sh <date>`: stop APIs â†’ `pg_restore --clean` â†’ untar media â†’ start â€” the restored stack is healthy. — WRITTEN, `sh -n` ok; needs the running stack
- [ ] `docker-compose.sqlserver.yml` overlay (SQL Server instead of postgres; `Database__Provider=SqlServer`) â€” `docker compose -f ... -f ... config` passes. — WRITTEN; `docker compose config` ask-gated
- [x] Add `deploy/single-host/.env` and `deploy/single-host/backups/` to `.gitignore` â€” `git status` does not show them. — .gitignore (git check-ignore confirms)

## Deployment â€” Kubernetes

- [x] `k8s/kustomization.yaml` (common app resources) + `k8s/postgres/kustomization.yaml` (StatefulSet + Service) â€” `make k8s-apply` (kind) output is unchanged. — k8s/base/kustomization.yaml (moved: a kustomization cannot contain its overlay), k8s/postgres/kustomization.yaml; kind files untouched
- [x] `k8s/production/kustomization.yaml`: base refs, `images:` â†’ GHCR, Production `configMapGenerator`, `secretGenerator` from `secret.env` â€” `kubectl kustomize k8s/production` renders with `secret.env.example` copied. — k8s/production/kustomization.yaml; renders only with --load-restrictor=LoadRestrictionsNone
- [x] `k8s/production/secret.env.example` + `.gitignore` entry for `secret.env` â€” no real secret in git. — k8s/production/secret.env.example, .gitignore
- [x] Patches: `imagePullPolicy: Always`, optional `imagePullSecrets`, migrate `initContainer` on the 4 data APIs (Identity gets the bootstrap keys), resource limits â€” the rendered Deployments show the init containers. — k8s/production/patches/*.yaml (render shows 4 init containers)
- [x] `postgres-init-job.yaml` (DBs + roles, same script as single host), `media-pvc.yaml`, `rabbitmq-pvc.yaml` â€” no `emptyDir` is left for media or RabbitMQ. — k8s/production/postgres-init-job.yaml, postgres-init.sh, media-pvc.yaml, rabbitmq-pvc.yaml, patches/media-volume.yaml, rabbitmq-volume.yaml (render has no emptyDir)
- [x] `ingress-patch.yaml`: host + `tls.secretName: wordbuddy-tls`, class configurable, body size â€” the rendered Ingress has a `tls:` block. — k8s/production/ingress-patch.yaml (render shows tls:)
- [x] `networkpolicy.yaml`: default deny ingress + the allow rules listed in the plan â€” postgres/rabbitmq are reachable only from API pods. — k8s/production/networkpolicy.yaml (not enforced on kind)
- [x] `backup-cronjob.yaml` + `backup-pvc.yaml` (nightly `pg_dump` of 4 DBs, retain 14 days) â€” `kubectl create job --from=cronjob/...` writes 4 dumps. — k8s/production/backup-cronjob.yaml, backup-pvc.yaml (render ok; job run needs a cluster)
- [x] Makefile: `k8s-prod-render`, `k8s-prod-apply VERSION=`, `k8s-prod-health` (shows the kube context and asks for confirmation) â€” existing targets unchanged. — WordBuddy/Makefile (k8s-prod-render ran ok)

## CI

- [x] `.github/workflows/images.yml`: tag `v*` + manual trigger, 6-image matrix, BuildKit secret from `GITHUB_TOKEN`, tags `X.Y.Z` / `X.Y` / `sha-<short>`, OCI labels â€” a manual run pushes all 6 images to GHCR. — .github/workflows/images.yml (not run: needs GitHub)

## Docs

- [x] `docs/deployment/single-host.md` runbook: host prerequisites (Linux, â‰¥ 2 GB, Docker, DNS, ports 80/443), GHCR login, `.env`, `up -d`, health check, first admin login + password change, release upgrade, backup, restore, firewall check â€” a fresh reader can follow it top to bottom. — docs/deployment/single-host.md
- [x] `docs/deployment/kubernetes.md` runbook: cluster prerequisites (ingress controller, storage class, optional cert-manager), secret, TLS, `make k8s-prod-apply`, health, backup/restore â€” one path from empty namespace to healthy stack. — docs/deployment/kubernetes.md
- [x] `WordBuddy/README.md` + per-service READMEs: link to the runbooks and document `migrate` mode â€” links resolve. — WordBuddy/README.md, 5 service READMEs

## Tests

- [x] Shared.Infrastructure unit: `ForwardedHeadersExtensions_AddWordBuddyForwardedHeaders_*` â€” empty config = no trusted proxy; CIDR list parsed; invalid CIDR fails at startup. — Identity.UnitTests/Persistence/ForwardedHeadersExtensionsTests.cs (no Shared test project exists; ADR 0001 forbids a reference, so the tests sit in Identity unit tests)
- [x] Identity unit: `AdminBootstrapOptions` validation (missing email, short password, valid) â€” one `[Fact]` per case. — Identity.UnitTests/Persistence/AdminBootstrapOptionsTests.cs
- [x] Identity integration (SQL Server + Postgres): `AdminBootstrapper_Run_CreatesAdultAdminWhenNone`, `..._SkipsWhenAdminExists`, `..._FailsWhenNotConfigured`, `..._DoesNotLogSecrets` â€” all green on both providers. — Identity.IntegrationTests/MigrationAndBootstrapTests.cs (SQL Server green; Postgres not run)
- [x] Identity, Content, Quiz, Progress integration: `MigrationRunner_Run_AppliesAllMigrations` on an empty DB (both providers) â€” `GetPendingMigrations()` is empty afterwards and the exit code is 0. — 4 x MigrationRunner tests (SQL Server green; Postgres not run)
- [x] Each API integration: `Production` environment â†’ `GET /swagger/index.html` = 404 â€” green for the 5 services. — ProductionEnvironmentTests.cs in Identity/Content/Quiz/Progress; Notification has no integration tests, so not covered
- [ ] Identity integration: behind a trusted proxy, two `X-Forwarded-For` IPs get separate login rate-limit partitions â€” the second IP is not throttled by the first. — NOT DONE: Identity has no anonymous rate-limit policy (login/register are not limited), so the plan's login-partition test has no target; see report
- [ ] `e2e/api`: smoke suite against the local production Compose stack (`SITE_ADDRESS=localhost`) â€” HTTPS 200 on `/`, `/api/auth/login` works with the bootstrap admin, HTTP redirects to HTTPS, ports 5432/5672/15672/5080-5084 are not reachable from the host. — NOT DONE: needs the running Compose stack (ask-gated)
- [ ] Backup/restore drill on the local production Compose stack: register a learner â†’ backup â†’ `down -v` â†’ restore â†’ the learner can log in â€” result recorded in `test-report.md`. — NOT DONE: needs docker compose (ask-gated), belongs to /test
- [ ] Kubernetes on kind: `kubectl kustomize k8s/production` renders, then apply with local images retagged as GHCR names â€” all 5 APIs ready; result recorded in `test-report.md`. — NOT DONE: needs kind cluster, belongs to /test
- [ ] Regression: `make up`, `make up-postgres`, `make k8s-apply` and `aspire run` still start and stay healthy â€” no diff in their behaviour. — NOT DONE: docker/aspire runs are ask-gated; belongs to /test
