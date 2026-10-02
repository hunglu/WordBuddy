# Review: New Feature to build up vocabulary and check up (needs-fixes round)

PR: #10 · Round 1 · Reviewed commit: 580e022 · 2026-10-02T08:50:13+07:00

## Verdict
Changes requested. The nginx fix is correct for the five routes it touches. But neither nginx nor the k8s Ingress routes `/api/vocabulary/*`, which is the feature's own API, so the vocabulary UI flow will still fail behind Docker or Kubernetes once login works.

## Findings
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

## Plan conformance
This round implements the two fixes named in `test-report.md` → Failures (nginx path, `/health` endpoints) and the test-only JSON fix. The Makefile change was added on Sam's request and is out of the plan's scope, but harmless. It also misses the third routing gap (`/api/vocabulary`, findings 1–2), which will surface as the next E2E UI failure. Live verification (container rebuild, E2E) is deferred to `/test`.
