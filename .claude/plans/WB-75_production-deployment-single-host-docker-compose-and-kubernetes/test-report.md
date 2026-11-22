# Test report: Production deployment: single host (Docker Compose) and Kubernetes

PR #76 · tested at HEAD `3c7e66f` (`origin/main` already merged, nothing new) · 2026-11-22

## Backend unit/integration

Each run created and dropped its own databases. Quiz runs on Shared.Infrastructure 1.4.0; its factory starts the host in Development (nit 13 covered).

| Service | Suite | SQL Server (localdb) | PostgreSQL |
| --- | --- | --- | --- |
| Identity | Unit | 262 pass | 262 pass |
| Identity | Integration | 63 pass | 63 pass |
| Content | Unit | 481 pass | 481 pass |
| Content | Integration | 234 pass | 218 pass, 16 skipped |
| Quiz | Unit | 17 pass | 17 pass |
| Quiz | Integration | 9 pass | 9 pass |
| Progress | Unit | 472 pass | 472 pass |
| Progress | Integration | 105 pass | 105 pass |
| Aspire AppHost model (`Category!=Integration`) | - | 39 pass | n/a |

The 16 Content skips on PostgreSQL are by design (`SqlServerOnlyFact`: SQL Server migration replay, Postgres has one InitialCreate). Failed: 0 everywhere.

## E2E API

Skipped — services not running. `ProductionSmokeTests` also need `E2E_PRODUCTION_URL`. Not re-run: the single-host stack needs GHCR images or a GitHub PAT for a local build.

Coder-run evidence (from `tasks.md`, not re-run here):

| Check | Result |
| --- | --- |
| Single-host stack, full start | 5 APIs healthy, `health.sh` 5 OK |
| `ProductionSmokeTests` on local stack (`SITE_ADDRESS=localhost`) | 11 pass |
| Backup / restore drill | learner registered, backup, `down -v`, restore, learner login 200 |
| `postgres/init.sh` second run | no change |
| Kubernetes on kind (retagged local images) | 7 Deployments rolled out, admin login 200, backup job ok |
| `make k8s-apply DB=postgres` on kind | healthy |

## E2E UI

Skipped — no user-visible UI change (only `nginx.conf` forwarded headers); services not running.

## Not run (accepted by Sam)

- Login rate-limit test (dropped; no login rate limit exists, separate proposal).
- `make up`, `make up-postgres`, `aspire run` (image build needs a GitHub PAT).

## Failures

None.

## Open nits (follow-ups, not blockers)

| # | Where | Nit |
| --- | --- | --- |
| A | `MigrationFailureLog.cs:33` (4 services) | SQL Server duplicate-key error (2601/2627) message can contain the key value (an email). Log only the number |
| B | `deploy/single-host/sqlserver/init.sh` | Passwords are not checked as `^[A-Za-z0-9]{16,}$`; a `'` or an empty value breaks the batch |
| C | `docs/deployment/kubernetes.md:30` | "from the repository root" is wrong; say "from the folder where you ran `git clone`" |

## Merge guard

1. `review.md` verdict Approve; reviewed commit `7b0efeb` is an ancestor of HEAD (`3c7e66f`): pass.
2. `git merge-tree --write-tree 7b0efeb origin/main`, then `git diff --name-only <tree> HEAD`: empty output (nothing changed outside the reviewed code + main), pass.
3. Every suite that ran had zero failures. E2E API and E2E UI were skipped (a skipped suite is not a pass by the guard); merged on the instruction from Sam to use the coder-run evidence above. Sam decides this exception.

## Verdict

Approved — merged into main
