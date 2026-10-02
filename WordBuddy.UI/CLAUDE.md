<!-- Frontend-specific rules only. Project-wide context (domain, workflow, docs map) is in
     ../CLAUDE.md; backend rules in ../WordBuddy/CLAUDE.md. Stable rules only — no run logs. -->

# WordBuddy.UI

React + TypeScript SPA served by Vite (versions: `package.json`), talking to the 5 backend
microservices in `../WordBuddy`. Follow the `react-typescript` skill for the
  exact shapes.

## Stack (as actually installed)

| Layer | Library | Version |
| --- | --- | --- |
| Build | Vite | 8.x |
| UI | React | 19.x |
| Routing | React Router | 7.x |
| Server state | TanStack Query | 5.x |
| Client state | Zustand | 5.x |
| Styling | TailwindCSS | 4.x (via `@tailwindcss/vite`, no `tailwind.config.js`/PostCSS needed) |
| Animation | Framer Motion | 13.x |
| Forms | React Hook Form + Zod | 7.x / 4.x |
| HTTP | Axios | 1.x |

## Backend connectivity — no `VITE_API_URL`

The backend is 5 independent microservices with no shared origin — there is no single "backend
URL" to point at. Instead, `apiClient` (`src/api/client.ts`) uses a **relative** base URL
(`/api`), and routing to the right service happens by path prefix:

- **Dev**: `vite.config.ts`'s `server.proxy` maps `/api/auth` → Identity (`:5080`),
  `/api/lessons`+`/api/media`+`/api/vocabulary` → Content (`:5081`), `/api/quiz` → Quiz (`:5082`),
  `/api/progress` → Progress (`:5083`).
- **Docker/Kubernetes** (Phase 4): this app's own Nginx (see `nginx.conf`) does the same
  path-based `proxy_pass` routing at a single origin (`:80`), and the `k8s/` Ingress in the
  backend repo does it again one layer up for the catch-all `/` route to this service — the
  frontend code itself never changes between dev/Docker/Kubernetes, only who's doing the
  routing.

All 5 backend services exist as of Phase 4 (Identity/Content/Quiz/Progress fully implemented,
Notification a scaffold with no public endpoints yet). Every page that calls a backend endpoint
still handles the TanStack Query `isError` state gracefully (no crash, no blank screen) — keep
doing that for any new page, since Notification and any not-yet-running service will still
502/ECONNREFUSED.

## Folder structure

```
src/
├── api/            Axios functions per backend service (client.ts, auth.ts, lessons.ts, progress.ts)
├── components/      Reusable UI components
│   ├── lessons/     VocabularyList, GrammarRuleList, DailyPhraseList
│   ├── ProtectedRoute.tsx
│   └── CountUpStat.tsx
├── layouts/         PublicLayout (auth pages), AppLayout (sidebar + protected pages)
├── pages/           One file per route
├── store/           authStore.ts (Zustand, persisted to localStorage)
├── types/           index.ts — interfaces mirroring backend DTOs, union types for enums
└── utils/           jwt.ts — client-side JWT decode/expiry check (no verification, display only)
```

## Conventions

### TypeScript — strict, no `enum`

`tsconfig.app.json` sets both `strict: true` and `erasableSyntaxOnly: true`. The latter
disallows real TS `enum` (non-erasable) — model backend enums as union string literal types
(`type AgeGroup = 'Child' | 'Adult'`) instead, matching the JSON strings the API actually sends
(`JsonStringEnumConverter` on the backend).

### Server state — TanStack Query only

Never use raw `useEffect` + `fetch`/`axios` for API calls. Every API call goes through a
`useQuery`/`useMutation` hook. Query keys follow `['resource', ...params]`, e.g.
`['lessons', type, level]`.

### Client state — Zustand only

`authStore` holds only the auth session (token, user, `login`/`logout`). Server data (lessons,
progress) stays in TanStack Query's cache — never copy it into Zustand.

### Animations — Framer Motion only

Page entrances use `motion.div` with a simple fade/slide; route transitions go through
`AnimatePresence` in `App.tsx`. No CSS `transition`/`animation` for anything interactive, no
inline `style` for animation values.

### Styling — Tailwind utilities + CSS Modules

- **Tailwind utilities first** for layout, spacing and one-off styling in JSX.
- **CSS Modules** (`Component.module.css`, next to the component) for component-scoped styles
  that would be unreadable as a long class string. Import as `styles` and use
  `className={styles.card}`; class names are camelCase. A module that uses Tailwind (`@apply`,
  `theme()`, `--color-*`) starts with `@reference "<relative path>/index.css";` — never
  `@import "tailwindcss"` again (that duplicates the whole framework per module).
- **Global CSS lives only in `src/index.css`**: `@import "tailwindcss"`, the `@theme` design
  tokens, and base element rules. No other global `.css` files; no `:global` in modules except
  for a documented third-party override.
- Colours, radii, shadows and fonts always come from theme tokens (`var(--color-primary)` in a
  module, `bg-primary` in JSX) — never raw hex or palette classes.
- No inline `style={{}}` (animation values via Framer Motion's `style`/`animate` props are the one
  exception — those aren't CSS). No Sass/Less/Stylus (unsupported with Tailwind v4).
- **Bundling:** `vite build` emits **one minified CSS file** for the whole app
  (`build.cssCodeSplit: false` in `vite.config.ts`) — every module and `index.css` concatenated,
  scoped class names hashed, unused Tailwind utilities never generated. Don't add per-route CSS
  splitting without an ADR.

### Auth

`src/api/client.ts` attaches `Authorization: Bearer <token>` by reading
`useAuthStore.getState()` directly (outside React — the store instance, not the hook). On a 401
response it calls `logout()` and hard-redirects to `/login`.

`ProtectedRoute` decodes the JWT's own `exp` claim (`src/utils/jwt.ts`) rather than trusting a
separately-stored expiry value — the two could drift out of sync; the token itself is the source
of truth. Expired or missing token → `logout()` + redirect to `/login`.

### End-to-end / BDD testing

This project has no in-tree test tooling of its own (no `test` script, no Vitest/Jest). Browser
E2E coverage lives outside `src/`, in `../e2e/ui` (TypeScript Playwright + `playwright-bdd`,
Gherkin `.feature` files) — see that project's `README.md`. It's written/run by the `tester`
subagent as part of the repo's `/test` workflow command, driving the app the same way a learner
would rather than testing components in isolation.
