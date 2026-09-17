# WordBuddy.UI

Frontend for **WordBuddy** — an English learning web app for children and adults. React 19 +
TypeScript SPA served by Vite.

Backend: [`github.com/hunglu/WordBuddy`](https://github.com/hunglu/WordBuddy) (separate repo —
5 independent microservices).

## Tech stack

![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-strict-3178C6?logo=typescript&logoColor=white)
![Vite](https://img.shields.io/badge/Vite-8-646CFF?logo=vite&logoColor=white)
![TailwindCSS](https://img.shields.io/badge/Tailwind-4-06B6D4?logo=tailwindcss&logoColor=white)
![TanStack Query](https://img.shields.io/badge/TanStack%20Query-5-FF4154?logo=reactquery&logoColor=white)
![Zustand](https://img.shields.io/badge/Zustand-5-black)

## Folder structure

```
src/
├── api/            Axios functions per backend service
├── components/     Reusable UI (incl. components/lessons/ for lesson-detail rendering)
├── layouts/        PublicLayout, AppLayout
├── pages/          One file per route
├── store/          Zustand authStore (persisted)
├── types/          Interfaces mirroring backend DTOs
└── utils/          jwt.ts (client-side decode/expiry check)
```

## Getting started

```bash
npm install
npm run dev
```

Opens at `http://localhost:5173`. The dev server proxies `/api/*` to the backend services — see
[`CLAUDE.md`](./CLAUDE.md) for the exact routing and dev ports each service needs to run on.
Only the Identity service is implemented so far; start it from the backend repo before testing
login/register:

```bash
dotnet run --project src/Services/Identity/WordBuddy.Identity.Api --urls http://localhost:5080
```

## Routes

| Route | Access | Description |
|---|---|---|
| `/login` | Public | Log in with email/password |
| `/register` | Public | Create an account (Child/Adult) |
| `/` | Protected | Dashboard |
| `/lessons` | Protected | Browse lessons, filter by type/level |
| `/lessons/:id` | Protected | Lesson content + "Mark as Complete" |
| `/progress` | Protected | Completion stats + completed lesson list |

Protected routes redirect to `/login` when there's no token or the JWT has expired.

## Environment variables

None currently — the backend has no single origin (5 independent services), so the app talks to
`/api/*` (relative) and lets the dev proxy / production ingress route by path. See
[`CLAUDE.md`](./CLAUDE.md#backend-connectivity--no-vite_api_url) for why.

## Build

```bash
npm run build   # tsc -b (strict type-check) && vite build
```

## Running in Docker / Kubernetes

Multi-stage `Dockerfile`: `node:20-alpine` builds the static bundle, `nginx:alpine` serves it.
`nginx.conf` does SPA fallback (`try_files ... /index.html`), 1-year cache on hashed assets,
no-cache on `index.html`, gzip, and path-based `proxy_pass` to each backend service — the same
routing the Vite dev proxy does, just at the Nginx layer instead:

| Path prefix | Upstream |
|---|---|
| `/api/auth` | `identity-api:8080` |
| `/api/lessons`, `/api/media` | `content-api:8080` |
| `/api/quiz` | `quiz-api:8080` |
| `/api/progress` | `progress-api:8080` |

Upstream hostnames are resolved per-request (`resolver 127.0.0.11 valid=10s;` + a `set $var
http://host:port;` before each `proxy_pass`), not once at config load — otherwise an upstream
container/pod that isn't up yet when Nginx starts would stop its workers from binding at all,
not just 502 that one route.

```bash
docker build -t wordbuddy-ui:dev .
docker run -p 3000:80 wordbuddy-ui:dev
```

In the backend repo's `docker-compose.yml` / `k8s/` manifests this becomes the `wordbuddy-ui`
service — see `WordBuddy/CLAUDE.md`'s "Running in Docker / local Kubernetes" section for the
full picture (kind cluster, Ingress catch-all `/` route to this service).
