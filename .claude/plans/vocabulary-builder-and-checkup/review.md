# Review: New Feature to build up vocabulary and check up (needs-fixes round)

PR: #10 · Round 3 · Reviewed commit: 7c1ca69 · 2026-10-02T11:04:32+07:00

## Verdict
Approve. The round-2 blocker is fixed: all six `/api/*` nginx locations are now `^~ /api/<x>` with no trailing slash, so both the bare and the sub-path calls reach the right service. The whole-PR pass found no blockers or majors.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `WordBuddy.UI/nginx.conf:31-72` | A prefix without a slash also matches siblings such as `/api/authx` or `/api/progress-old`. No such route exists today, and the matched service would just return 404, so this is harmless. | None needed. If a colliding prefix is ever added, use `location = /api/<x>` plus `^~ /api/<x>/`. |
| 2 | nit | `*/WordBuddy.*.Api.csproj` (5 files), `WordBuddy.Shared.Infrastructure.csproj` | Roughly 50 lines of churn per file are line-ending changes. Ignoring whitespace and CR, the real change is only the HealthChecks EF package, Shared.Infrastructure 1.0.2 and the `FrameworkReference`. | Optional: add a `.gitattributes` rule for `*.csproj` so future diffs stay readable. |

Verified for round 3 (by reading; `nginx -t` was not run):
- **Routing of every call in `src/api/*.ts`** (relative to `baseURL: '/api'`):
  - `/auth/login` and `/auth/register` go to identity-api.
  - `/lessons` and `/lessons/{id}` go to content-api.
  - `/vocabulary`, `/vocabulary/mine`, `/{id}`, `/{id}/share`, `/shared`, `/shared/{id}/add-to-mine`, `/check?count=`, `/moderation/pending` and `/moderation/{id}` go to content-api.
  - `/progress`, `/progress/vocabulary-recall` (GET and POST) go to progress-api.
  - The `/api/quiz` and `/api/media` blocks match the same way.
  - No two `/api/*` prefixes overlap, so the longest-prefix choice is unambiguous.
- **Static-file regex:** `^~` on the longest matching prefix stops nginx from evaluating regex locations, so `/api/media/x.png` (or any `/api/...` ending in `.js`/`.css`) is proxied and never served from disk.
- **`proxy_pass $<x>_upstream;`:** the variable has no URI part, so nginx forwards the original request URI and query string unchanged (`/api/vocabulary/check?count=5` arrives as-is). The `resolver` and `set` lines are unchanged.
- **No regressions:** `location = /index.html` (no-cache), the static regex and the SPA fallback `location /` are unchanged. `client_max_body_size 50m` stays on `/api/media` only. The diff touches only the six location lines.

Whole-PR pass (`origin/main...7c1ca69`, 31 files):
- **nginx and Ingress:** `/api/vocabulary` is routed to content-api in both. All `/api/*` locations use `^~` with no trailing slash.
- **k8s probes:** readiness uses `/health/ready` and liveness uses `/health`.
- **Health helper:** liveness has no dependency checks, readiness runs the checks tagged `ready`, and both endpoints allow anonymous access. The EF health-check package version matches EF Core 10.0.12.
- **Service wiring:** Program.cs and WebApplicationExtensions are wired in all 5 services, and the usings are ordered.
- **Rest of the diff:** the integration-test JSON options fix, the Makefile `publish-shared` loop, and the CLAUDE.md route-table edits (Sam-approved) are all fine.
- **Conventions:** no secrets, no cross-service references, no `.Result`/`.Wait()`.

## Plan conformance
Fix round 3 addresses round-2 finding 1 and nothing else (nginx.conf plus the proposal bookkeeping). All of the needs-fixes items from `test-report.md` and the review findings from rounds 1 and 2 are now covered. Live verification (container rebuild, `nginx -t`, E2E) is left to `/test`. Board sync is skipped because `issue: none`.

## Previous rounds

PR: #10 · Round 2 · Reviewed commit: d41853a · 2026-10-02T10:43:53+07:00

### Verdict
Changes requested. Findings 2-5 from round 1 are fixed. Finding 1 is only partly fixed: every nginx `/api/` location ends in a slash, so the calls the UI makes without a trailing slash (`POST /api/vocabulary`, `GET /api/lessons`, `GET/POST /api/progress`) still go to the SPA fallback.

### Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | blocker | `WordBuddy.UI/nginx.conf:38,53,60,67` | Nginx matches `^~ /api/vocabulary/` as a literal string prefix, so `/api/vocabulary` with no slash does not match it. No other location matches either: the regex only applies to static extensions, and `= /index.html` is an exact match. The request falls through to `location / { try_files $uri $uri/ /index.html; }`. The UI makes these calls without a slash: `vocabulary.ts:11` `POST /vocabulary` (add word: nginx returns 405 for a POST to static content), `lessons.ts:5` `GET /lessons` (returns index.html with status 200, so the lessons list gets HTML instead of an array), `progress.ts:11,15` `POST/GET /progress` (405 / HTML). In the container, the add-word flow, the lessons page and the progress dashboard all break. This gap was already in the `/api/lessons/` and `/api/progress/` blocks before this PR, and round 1 did not report it for those two. The Vite dev proxy and the k8s Ingress `Prefix` paths are not affected. | Drop the trailing slash from the prefix: `location ^~ /api/vocabulary`, `^~ /api/lessons`, `^~ /api/progress` (and `/api/quiz`, `/api/media`, `/api/auth` for consistency). Alternatively, add an exact `location = /api/<x>` block next to each one. |

Verified fixed (round-1 findings):
- **R1-1 (partial):** `^~ /api/vocabulary/` routes to `content-api:8080`, and the CLAUDE.md route tables include `/api/vocabulary`. `/vocabulary/mine`, `/shared`, `/shared/{id}/add-to-mine`, `/check`, `/moderation/*`, `/{id}` and `/{id}/share` are covered. The bare path is not (see finding 1).
- **R1-2:** `ingress.yaml` adds `/api/vocabulary` with `pathType: Prefix` to content-api:8080. A k8s Prefix match covers both `/api/vocabulary` and `/api/vocabulary/...`.
- **R1-3:** every `/api/` location uses `^~`, so the static-extension regex no longer takes `/api/media/*.png|jpg`.
- **R1-4:** all 5 Deployments use readiness `/health/ready` and liveness `/health` on port 8080, with initial delays of 10s/15s unchanged. `/health` has no dependency checks, so a down DB, or a slow migration at startup, only marks the pod NotReady. Kubernetes does not restart it, so the pods won't flap.
- **R1-5:** in all 5 `WebApplicationExtensions.cs` files, `using Serilog;` now comes first.

The fix round introduced no new issues. `nginx -t` and YAML lint were not run; I reviewed by reading.

### Plan conformance
Fix round 2 addresses the round-1 findings only. CLAUDE.md edits were Sam-approved. No out-of-scope changes. Board sync is skipped because `issue: none`.

---


PR: #10 · Round 1 · Reviewed commit: 580e022 · 2026-10-02T08:50:13+07:00

### Verdict
Changes requested. The nginx fix is correct for the five routes it touches. But neither nginx nor the k8s Ingress routes `/api/vocabulary/*`, which is the feature's own API, so the vocabulary UI flow will still fail behind Docker or Kubernetes once login works.

### Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | blocker | `WordBuddy.UI/nginx.conf:30-65` | There is no `location /api/vocabulary/`. `src/api/vocabulary.ts` calls `/api/vocabulary`, `/api/vocabulary/mine`, `/shared`, `/check` and `/moderation/pending`. In dev, `vite.config.ts:25` proxies these to Content, but in the container they fall through to the SPA fallback `location /`. A GET returns `index.html` with status 200, so TanStack Query gets an HTML string where it expects an array, and the page either crashes or shows nothing. A POST to `/api/vocabulary` (add word) returns 405 from static serving. The `vocabulary.feature` UI scenario will still fail after the login fix. | Add `location /api/vocabulary` (no trailing slash, so it also matches `POST /api/vocabulary`) with `set $content_upstream http://content-api:8080; proxy_pass $content_upstream;` and the same headers. Add `/api/vocabulary` to `WordBuddy.UI/README.md` and to the routing table in `WordBuddy/CLAUDE.md` (that file is ask-gated, so propose the change to Sam). |
| 2 | blocker | `WordBuddy/k8s/ingress.yaml:15-43` | Same gap one layer up: the Ingress has no `/api/vocabulary` path, so on kind those requests go to the catch-all `/` path and reach the UI pod. The UI's nginx then needs finding 1 to forward them. | Add a `/api/vocabulary` Prefix path pointing to `content-api:8080`. |
| 3 | major | `WordBuddy.UI/nginx.conf:17` (with `:45`) | This bug predates the PR, but this location block is in scope. The regex `location ~* \.(js\|css\|svg\|png\|jpg\|jpeg\|gif\|woff2?)$` takes precedence over the prefix `location /api/media/` because the prefix has no `^~`. So `GET /api/media/<x>.png` is served with `try_files $uri =404` from the static root and returns 404, never reaching Content. Image media is broken behind nginx. (`.mp3` is not in the list, so audio still works.) | Change all `/api/...` prefix locations to `location ^~ /api/...`, so a matching prefix stops nginx from checking the regex locations. |
| 4 | nit | `WordBuddy/k8s/*-deployment.yaml` (e.g. `content-deployment.yaml:46-57`) | Probes still hit `/swagger/index.html`, even though the services now expose `/health` and `/health/ready`. | Follow-up: readiness → `/health/ready`, liveness → `/health`. |
| 5 | nit | `*/Extensions/WebApplicationExtensions.cs:1` (all 5 services) | `using WordBuddy.Shared.Infrastructure.Health;` was placed before `using Serilog;`, which breaks the alphabetical order of the existing usings. | Reorder. |

Verified, no issue found:
- **nginx `proxy_pass`:** a variable with no URI passes the original request URI and query string through unchanged. That fixes `/api/auth/login` → `/api/auth/login`. The `resolver 127.0.0.11` and `set` lines are unchanged, and the SPA fallback is unaffected.
- **Health helper:** `/health` uses `Predicate = _ => false` (liveness only, no dependency checks). `/health/ready` runs the checks tagged `ready`. Both endpoints are `AllowAnonymous`. `MapWordBuddyHealthChecks` is called before `MapControllers` in all 5 services. There is no `AddRateLimiter`/`UseRateLimiter` anywhere in `src`, so rate limiting doesn't affect these endpoints (the rate-limiting middleware itself is still missing, but that is a separate gap). The helper has XML docs.
- **Versions and references:** `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.12 matches `Microsoft.EntityFrameworkCore.SqlServer` 10.0.12 in all four Infrastructure projects. Shared.Infrastructure is 1.0.2 in all 5 services. No cross-service project references were added. `FrameworkReference Microsoft.AspNetCore.App` is the correct way to get `MapHealthChecks` in a class library.
- **Notification:** registers health checks with no ready checks, so `/health/ready` reports Healthy. This is acceptable for a scaffold, and the code comments on it.
- **Integration test fix:** a test-only change. `JsonSerializerDefaults.Web` plus `JsonStringEnumConverter` matches the API's serialization.
- **Makefile `publish-shared`:** the per-file loop with `|| exit 1` is correct. `$$f` is escaped properly. Note that if `local-nuget-feed/` is empty, the unexpanded glob is pushed and the push fails loudly, which is acceptable.

### Plan conformance
This round implements the two fixes named in `test-report.md` → Failures (nginx path, `/health` endpoints) and the test-only JSON fix. The Makefile change was added on Sam's request and is out of the plan's scope, but harmless. It also misses the third routing gap (`/api/vocabulary`, findings 1–2), which will surface as the next E2E UI failure. Live verification (container rebuild, E2E) is deferred to `/test`.
