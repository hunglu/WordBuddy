# Review: Production deployment: single host (Docker Compose) and Kubernetes

PR: #76 · Round 2 · Reviewed commit: 7b0efeb · 2026-11-22T21:21:30+07:00

## Verdict

Approve — all 4 round-1 majors and 8 of 9 nits are fixed (nit 13 is for `/test`). The fixes add 3 new nits and no blockers or majors.

## Round 1 status

| # | Round 1 | Status | Verified |
| --- | --- | --- | --- |
| 1 | major | fixed | `optional: true` on both `Bootstrap__Admin__*` refs. Compose uses `${...:-}`. Bootstrapper: admin exists → exit 0 before options are read |
| 2 | major | fixed | Git mode `100755` on all 8 scripts, including the new `sqlserver/init.sh` |
| 3 | major | fixed | `MigrationFailureLog` logs the type chain, stack traces, `SqlState` / `SqlException.Number`. The provider message is logged only for a `DbException`. No exception object goes to the logger. The 4 copies are identical apart from the service name (ADR 0001). See new nit A |
| 4 | major | fixed | `sqlserver/init.sh`: one login + user per database, `db_owner` on its own database only. No API or migrate job uses `sa`. See new nit B |
| 5 | nit | fixed | `... \| sed \| kubectl apply -f -`. If the render fails, `kubectl` gets empty input and exits non-zero, so the failure still shows |
| 6 | nit | fixed | Header points to `make k8s-prod-render` |
| 7 | nit | fixed | `cd ../..` in section 3 is right. See new nit C for the section 2 comment |
| 8 | nit | fixed | `sqlcmd` runs inside the container with `$MSSQL_SA_PASSWORD`; `mkdir -p` first |
| 9 | nit | fixed | Allow-list is `[ingress-nginx, traefik]`; k3s case explained in the runbook |
| 10 | nit | fixed | "02:00 UTC" in both runbooks, the CronJob and the diagram |
| 11 | nit | fixed | Backup step comes first in the upgrade section |
| 12 | nit | fixed | All 5 SHAs match their tags (checked with `gh api repos/<repo>/git/ref/tags/<tag>`; all are `commit` objects): checkout v4.4.0, setup-buildx v3.12.0, login v3.7.0, metadata v5.10.0, build-push v6.19.2 |
| 13 | nit | for /test | Quiz suites + one Quiz Development start |

Checked and fine:

| Area | Result |
| --- | --- |
| Password in migration logs | Npgsql and SqlClient connect/auth messages name user, host or database, never the password. A malformed connection string throws `ArgumentException` (not a `DbException`), so its message is withheld |
| `MigrationFailureLogTests` | Sound. Unreachable port 1 with a known password; the capturing logger also records `exception.ToString()`, so a passed exception object would fail the test. Asserts exit 1, error code text and a `MigrateAsync` frame |
| `init.sh` idempotency | `IF ... IS NULL` guards on database, login and user. `ALTER ROLE ... ADD MEMBER` is a no-op when already a member. `ALTER LOGIN` resets the password, which also makes rotation work. `-b` + `set -eu` stop on the first error |
| `CHECK_POLICY = OFF` | Accepted. Hex secrets (`openssl rand -hex 24`) have no upper case and fail the Windows complexity rule. Length gives the strength. `ALTER LOGIN` keeps the OFF setting |
| `/tmp/setup.sql` | Holds only the template (quoted heredoc), never a password |
| `optional: true` | A fresh database with missing keys still fails the init container (exit 1), which is the wanted signal |

## Findings (round 2)

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| A | nit | `WordBuddy.Identity.Api/Extensions/MigrationFailureLog.cs:33` (same line in Content, Quiz, Progress) | On SQL Server a duplicate-key `SqlException` (2601/2627) message contains the key value. Scenario: someone registers with the address later set as `BOOTSTRAP_ADMIN_EMAIL`; no admin exists, so the bootstrapper inserts, the unique index on `Users.Email` fails, and the log line holds "The duplicate key value is (that email)". Same for a future migration that adds a unique index over existing user data. Npgsql does not put `Detail` in `Message` by default, so Postgres is not affected | For `SqlException` 2601/2627 log only the number (withhold the message), or cut the message at "The duplicate key value is". Optional: in `AdminBootstrapper`, check `Users.AnyAsync(u => u.Email == email)` first and fail with a clear, email-free message |
| B | nit | `WordBuddy/deploy/single-host/sqlserver/init.sh:17,19,30-33` | Passwords go into SQL as raw `sqlcmd` variables. The "letters and digits only" rule is a comment, not a check. A `'` breaks the batch (or runs extra SQL); a missing `.env` line gives an empty string and creates a login with a blank password (Compose only warns). The Postgres script quotes with `%L` and is safe | At the top of `setup()`: fail unless `printf '%s' "$password" \| grep -Eq '^[A-Za-z0-9]{16,}$'`. Or use `${IDENTITY_DB_PASSWORD:?}` in the overlay |
| C | nit | `docs/deployment/kubernetes.md:30` | Comment says "from the repository root", but `WordBuddy/WordBuddy/k8s/production` is correct only from the folder that holds the clone. From the repo root the path is `WordBuddy/k8s/production` | Say "from the folder where you ran `git clone`" (as `single-host.md:26` does), or use `cd WordBuddy/k8s/production` from the repo root and adjust the comment |

## Plan conformance

- **Tasks:** 41/41 still reflected. The fix round touched only files already in the plan, plus the new `sqlserver/init.sh` and `MigrationFailureLog` + tests (direct answers to findings 3 and 4).
- **Accepted by Sam, not findings:** dropped login rate-limit test, partial regression, Quiz Shared 1.4.0 check moved to `/test`.
- **Out of scope:** nothing.
- **DB diagrams:** no EF migration in the diff.

## Previous rounds

### Round 1 (commit 9f1c5ac)

PR: #76 · Round 1 · Reviewed commit: 9f1c5ac · 2026-11-22T21:04:21+07:00

#### Verdict

Changes requested — 4 majors: a documented Kubernetes step breaks Identity, the runbook scripts are not executable, migration failures cannot be diagnosed, and the SQL Server overlay runs every API as `sa`. No blockers.

Security focus areas checked and fine:

| Area | Result |
| --- | --- |
| Secrets in git | None. `.env`, `secret.env`, `backups/`, `.rendered.yaml` are gitignored; examples hold `CHANGE_ME` only |
| Secrets in logs | Bootstrap never logs email or password (test covers it); migrate logs no connection string |
| Admin bootstrap | Always `Adult`, BCrypt 12, only when no admin exists, exit 1 when not configured; keys reach only the migrate container / init container |
| Forwarded headers | Empty config = `ForwardedHeaders.None`; invalid CIDR fails at startup; loopback defaults cleared. Caddy drops client `X-Forwarded-For`, so spoofing from outside does not work |
| Exposed ports | Single host: only Caddy publishes 80/443. Kubernetes: no NodePort/LoadBalancer added |
| NetworkPolicy | Default deny ingress; Postgres only from API pods + init/backup; RabbitMQ only from the 3 messaging APIs |
| Backup files | `umask 077` + `chmod 600`, day folders `0700` |
| CI | BuildKit secret (no build arg), `contents: read` + `packages: write`, semver + sha tags, OCI labels |
| Local dev paths | `docker-compose.yml`, kind manifests and existing Make targets unchanged; `nginx.conf` change is additive |

#### Findings

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | major | `WordBuddy/k8s/production/patches/migrate-init-containers.yaml:31-40`, `docs/deployment/kubernetes.md:82` | The runbook says "Remove `Bootstrap__Admin__Password` from `secret.env` and run `make k8s-prod-apply` again". The init container uses `secretKeyRef` without `optional: true`. Removing the line removes the key, so the new Identity pod fails with `CreateContainerConfigError`. The old pod keeps serving until its next restart or node drain; after that Identity is down (no login for anyone). | Add `optional: true` to both `Bootstrap__Admin__*` refs, or tell the runbook to keep the key with an empty value (`Bootstrap__Admin__Password=`). |
| 2 | major | `WordBuddy/deploy/single-host/scripts/health.sh`, `scripts/restore.sh` (git mode `100644`); `docs/deployment/single-host.md:61,80,107` | Both scripts are committed without the executable bit. On a fresh Linux clone `./scripts/health.sh` fails with "Permission denied", so the runbook breaks at step 5 (success criterion "fresh host by following one runbook"). | `git update-index --chmod=+x` on both scripts, or change the runbook to `sh scripts/health.sh` / `sh scripts/restore.sh`. |
| 3 | major | `WordBuddy.Identity.Api/Extensions/MigrationRunner.cs:32`, `WordBuddy.Content.Api/Extensions/MigrationRunner.cs:29`, `WordBuddy.Quiz.Api/Extensions/MigrationRunner.cs:29`, `WordBuddy.Progress.Api/Extensions/MigrationRunner.cs:29` | On failure only `exception.GetType().Name` is logged. A wrong role password, a missing database and a failing migration script all log the same `NpgsqlException`/`PostgresException`. First deploy and upgrades cannot be diagnosed from `docker compose logs` / `kubectl logs -c migrate`, which is what the runbooks tell the operator to read. Also conflicts with the backend rule "logged at Error with stack trace". | Log the exception object (stack trace) plus `SqlState` / SQL error number. Npgsql and SqlClient messages do not contain the password. A strict option: log `DbException.Message` + `SqlState` and the stack trace. |
| 4 | major | `WordBuddy/deploy/single-host/docker-compose.sqlserver.yml:58,66,74,82,87,92,97,102` | Every migrate job and API connects as `sa`. A flaw in any one API gives sysadmin over all four databases (children's data in Identity and Progress). The plan and ADR 0010 promise "one least-privilege owner role per service"; the SQL Server path silently drops it. | Create one login + user per database in `sqlserver-init` (db_owner on its own database only), or state this as an accepted limitation in ADR 0010 and the runbook's SQL Server section. |
| 5 | nit | `WordBuddy/Makefile:143-145` | `k8s-prod-apply` writes the rendered overlay, including the base64 `wordbuddy-secrets` Secret, to `k8s/production/.rendered.yaml` with default permissions and leaves it there. | Pipe into `kubectl apply -f -`, or write to `mktemp` and delete it after apply. |
| 6 | nit | `WordBuddy/k8s/production/kustomization.yaml:3` | Header says `kubectl kustomize k8s/production`; without `--load-restrictor=LoadRestrictionsNone` it fails. | Use the full command or point to `make k8s-prod-render`. |
| 7 | nit | `docs/deployment/kubernetes.md:30,56` | Section 2 does `cd WordBuddy/k8s/production`; section 3 then says `cd WordBuddy`, which does not exist from there. | Use `cd ../..` or start both from the repo root. |
| 8 | nit | `docs/deployment/single-host.md:132-133` | The SQL Server backup example uses `$SA_PASSWORD` from the host shell (not set; `.env` is not sourced) and puts the password on the command line. `/var/opt/mssql/backup` may not exist. | Run `sqlcmd` via `docker compose exec sqlserver sh -c '... -P "$MSSQL_SA_PASSWORD" ...'` and `mkdir -p` the folder first. |
| 9 | nit | `WordBuddy/k8s/production/networkpolicy.yaml:32` | `kube-system` in the controller allow-list lets every kube-system pod reach UI and APIs. | Keep only the namespace of the real controller; mention k3s (Traefik in kube-system) in the runbook. |
| 10 | nit | `WordBuddy/deploy/single-host/backup/crontab:2`, `docs/deployment/single-host.md:87` | "02:00" is container time, which is UTC. | Say "02:00 UTC" or set `TZ` on the backup container. |
| 11 | nit | `docs/deployment/single-host.md:76-83` | "Take a backup first" comes after the upgrade commands. | Put the backup command first in the code block. |
| 12 | nit | `.github/workflows/images.yml:30-60` | Third-party actions are pinned by major tag, not by commit SHA, in a job holding `packages: write`. | Pin `docker/*` actions to SHAs (Dependabot can update them). |
| 13 | nit | `WordBuddy/src/Services/Quiz/WordBuddy.Quiz.Api/WordBuddy.Quiz.Api.csproj:23` | Quiz jumps from `Shared.Infrastructure` 1.0.2 to 1.4.0 (skips 1.1–1.3 changes, pulls MassTransit transitively) while Kernel stays 1.0.1. Builds, but the Development path of Quiz was not run (`make up` not run). | `/test`: run the Quiz suites and one Quiz Development start. |

#### Plan conformance

- **Tasks:** 41/41 reflected in the diff. Login rate-limit test dropped and regression task partial — both accepted by Sam, not findings.
- **Deviations (accepted, sound):**
  - Kustomize base at `k8s/base/` with `--load-restrictor=LoadRestrictionsNone` (a kustomization cannot contain its overlay).
  - Duplicated Postgres init script with a `diff -q` check in both `k8s-prod-*` targets.
  - Forwarded-header tests in `Identity.UnitTests` (no shared test project; ADR 0001).
  - `MigrationRunner` is `public static` so integration tests can call it.
  - `k8s-prod-apply` uses `sed` on the rendered output instead of `kustomize edit set image` in a temp copy.
- **Gaps:** Swagger-off test missing for Notification (no integration test project). Postgres runs of the new integration tests not done yet (`/test`). `images.yml` never run.
- **Out of scope:** nothing outside the plan.
- **Known note:** on kind, `/api/*` through the UI service returns 502 (nginx `resolver 127.0.0.11`); the Ingress routes APIs directly, so browsers are not affected.
- **DB diagrams:** no EF migration in the diff, no diagram change needed.

#### Fix round 1

| # | Result | What was done |
| --- | --- | --- |
| 1 | fixed | `optional: true` on both `Bootstrap__Admin__*` refs in `migrate-init-containers.yaml`. Compose uses `${BOOTSTRAP_ADMIN_*:-}`. Both runbooks say the keys may be removed. Identity bootstrapper checked: admin exists + settings missing = exit 0; no admin + settings missing = exit 1 (existing tests cover both) |
| 2 | fixed | `git update-index --chmod=+x` on `scripts/health.sh`, `scripts/restore.sh`, `backup/*.sh`, `postgres/init.sh`, `k8s/production/postgres-init.sh` and the new `sqlserver/init.sh` |
| 3 | fixed | New `MigrationFailureLog` (per service, Api project) logs exception types, stack traces of the full chain, `PostgresException.SqlState` / `SqlException.Number`. The provider message is logged only when the chain holds a `DbException` (server messages hold user, database or host, never the password). Other exception types keep the message withheld, because e.g. an `ArgumentException` from a bad connection string can echo it. The exception object is never passed to the logger. New test `MigrationFailureLogTests` per service (unreachable DB, asserts code and stack trace present, password absent) |
| 4 | fixed | `sqlserver/init.sh` (idempotent) creates the 4 databases, one login + user per database (`wb_identity`, ...) with `db_owner` on its own database only. APIs and migrate jobs use them. `sa` stays only for health check and init. Verified in a real SQL Server container: init runs twice, `wb_identity` can create a table in its database and cannot open another. Runbook and `.env.example` updated |
| 5 | fixed | `k8s-prod-apply` pipes into `kubectl apply -f -`; no rendered Secret on disk |
| 6 | fixed | `kustomization.yaml` header points to `make k8s-prod-render` and names the load restrictor |
| 7 | fixed | `cd` paths in `kubernetes.md` |
| 8 | fixed | SQL Server backup command runs `sqlcmd` inside the container with `$MSSQL_SA_PASSWORD`, `mkdir -p` first |
| 9 | fixed | `kube-system` and `ingress` removed from the allow-list (default `ingress-nginx`, `traefik`); comment and runbook section 4 explain the k3s case |
| 10 | fixed | Documented as 02:00 UTC (not configurable; Alpine image has no tzdata) in both runbooks, CronJob and compose comments |
| 11 | fixed | Backup step first in the upgrade section |
| 12 | fixed | All 5 actions pinned by full commit SHA with version comment; SHAs read with `gh api repos/<owner>/<repo>/git/ref/tags/<tag>` (all `commit` objects) |
| 13 | for /test | Quiz unit tests run and pass (17) |
