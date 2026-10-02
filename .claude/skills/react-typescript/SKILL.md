---
name: "react-typescript"
description: "Use when writing, refactoring, reviewing or testing React (TypeScript) code: components, hooks, state, data fetching, forms, performance and tests."
---

# React + TypeScript

Opinionated defaults for production React. Follow the existing project's conventions first; apply these where the project has none. If a rule here conflicts with the repo, say so and follow the repo.

## 0. Before writing code

1. Read `package.json`, `tsconfig.json`, and the lint config. Detect: React version, build tool (Vite / Next.js / Remix / CRA), router, state/data libs, test runner, styling approach.
2. Match what's there. Do not introduce a new library (state, forms, styling, fetching) without stating why and asking.
3. For Next.js App Router: respect the Server/Client Component boundary. Default to Server Components; add `'use client'` only at the leaf that needs state, effects, or browser APIs.

## 1. Baseline stack (greenfield only)

- React 19, TypeScript `strict: true` + `noUncheckedIndexedAccess` + `exactOptionalPropertyTypes`
- Vite (SPA) or Next.js (SSR/SEO needs)
- TanStack Query for server state; Zustand only if real shared client state exists
- React Hook Form + Zod for forms and runtime validation of API responses
- Vitest + React Testing Library + `@testing-library/user-event` + MSW
- ESLint with `eslint-plugin-react-hooks` (incl. React Compiler rules) and `@typescript-eslint/strict`

## 2. Project structure — feature-first

```
src/
  app/            # providers, router, app shell
  features/
    users/
      api/        # fetchers + query hooks (useUsersQuery)
      components/ # feature UI
      hooks/      # feature logic hooks
      model/      # types, zod schemas, pure functions
      index.ts    # public API of the feature — import only from here
  shared/
    ui/           # design-system primitives, no business logic
    lib/          # framework-agnostic utilities
    api/          # http client, interfaces
```

- Features never import another feature's internals — only its `index.ts`.
- Pure logic lives in `model/` as plain functions (testable without React).

## 3. Components

- Function components only. Named exports. One exported component per file.
- Props typed with a `type` named `<Component>Props`; mark them `Readonly<>`. No `React.FC`.
- React 19: `ref` is a normal prop — do not use `forwardRef` in new code.
- Keep components presentational where possible; move logic into hooks (container/hook split = SRP).
- Composition over configuration: prefer `children` / slot props over boolean prop explosions (`isPrimary`, `isLarge`, `hasIcon`...). Use discriminated unions for mutually exclusive variants.
- Never define components inside components.
- Keys: stable IDs, never array index for reorderable/insertable lists.
- Every public component/hook gets a JSDoc comment.

## 4. State — pick the lowest tier that works

1. Derived value → compute during render (no state, no effect).
2. Local UI state → `useState` / `useReducer` (reducer when transitions are non-trivial; make it a pure, exported, unit-tested function).
3. URL state (filters, pagination, tabs) → search params.
4. Server state → TanStack Query. Never copy query data into `useState`.
5. Shared client state → Context (low-frequency changes) or Zustand (high-frequency / many consumers).

Treat all state as immutable. No mutation of props, state, or query cache objects.

## 5. Effects

`useEffect` is for synchronizing with external systems only (subscriptions, DOM APIs, non-React widgets). It is NOT for:
- deriving state from props/state → compute in render
- reacting to user events → do it in the event handler
- fetching data → use TanStack Query / framework loaders / `use()`
- resetting state on prop change → use a `key`

Every effect that subscribes must clean up. Every async effect must handle cancellation (`AbortController`).

## 6. Data fetching & dependency injection

- API clients are interfaces in `shared/api`, implementations injected via a Context provider. Components and hooks depend on the interface, never on `fetch`/axios directly. This keeps hooks testable without network mocks when needed.
- Validate responses at the boundary with Zod; types are inferred from schemas (`z.infer`).
- Query keys via a factory per feature (`userKeys.all`, `userKeys.detail(id)`).
- Handle all three states explicitly: loading, error, empty. Wrap route-level trees in an Error Boundary + `Suspense`.
- Mutations: use `useMutation` with invalidation; use `useOptimistic` / optimistic updates only with a rollback path.
- React 19 forms without RHF: `<form action>` + `useActionState` + `useFormStatus` is acceptable for simple cases.

## 7. Performance

- If the React Compiler is enabled, do NOT add `useMemo` / `useCallback` / `memo` manually unless profiling proves a need.
- Without the compiler: memoize only (a) values passed to memoized children, (b) effect dependencies, (c) measured-expensive computations. Don't memoize by default.
- Split code at route level with `lazy`. Virtualize lists > ~200 rows (TanStack Virtual).
- Avoid context values that change every render; split contexts by update frequency.
- Use `useTransition` / `useDeferredValue` for expensive non-urgent updates instead of debouncing renders.

## 8. Accessibility (non-negotiable)

- Semantic elements first (`button`, `a`, `label`, `nav`, `main`). No clickable `div`s.
- Every input has an associated label. Every icon-only button has an accessible name.
- Visible focus states; manage focus on route change and in dialogs.
- If RTL can't find it by role, it's probably not accessible — fix the markup, not the test.

## 9. Testing

- Test behavior, not implementation. Query priority: `getByRole` > `getByLabelText` > `getByText` > `getByTestId` (last resort).
- Use `userEvent.setup()`, not `fireEvent`.
- Pure functions (reducers, mappers, schemas) → plain unit tests.
- Hooks → `renderHook` with a test wrapper providing QueryClient + injected fakes.
- Network → MSW handlers or an injected fake API client. Never mock `fetch` ad hoc.
- New QueryClient per test with `retry: false`.
- Each component deliverable ships with at least: happy path, loading, error, and one interaction test.

## 10. Output requirements

When generating code:
- Complete, runnable files — no `// ...rest` placeholders.
- Include the test file alongside.
- No `any`, no non-null `!` without a comment justifying it, no `as` casts on API data (parse with Zod instead).
- Error handling on every async path.
- When refactoring, show a before/after diff.

## 11. Review checklist

Flag these when reviewing React code, ranked by severity:

- [ ] Effect used for derived state / event handling / data fetching
- [ ] Missing effect cleanup or race condition on async effects
- [ ] Server data duplicated into local state
- [ ] Mutation of state/props/cache
- [ ] Unstable or index keys on dynamic lists
- [ ] Components defined inside components
- [ ] Missing loading / error / empty states
- [ ] Unvalidated API data cast with `as`
- [ ] Accessibility: non-semantic interactive elements, missing labels
- [ ] Cross-feature deep imports
- [ ] Premature or useless memoization (or missing where a memoized child depends on it)
- [ ] Tests coupled to implementation (class names, internal state, test IDs everywhere)

## 12. Reference example

```ts
// features/users/model/user.ts
import { z } from 'zod';

export const userSchema = z.object({
  id: z.string(),
  name: z.string(),
  email: z.string().email(),
});
export type User = Readonly<z.infer<typeof userSchema>>;
```

```tsx
// shared/api/api-context.tsx
import { createContext, useContext, type ReactNode } from 'react';
import type { User } from '@/features/users/model/user';

/** Abstraction over the users backend; inject a fake in tests. */
export interface UsersApi {
  list(signal?: AbortSignal): Promise<readonly User[]>;
}

const UsersApiContext = createContext<UsersApi | null>(null);

/** Provides the UsersApi implementation to the tree. */
export function UsersApiProvider({ api, children }: Readonly<{ api: UsersApi; children: ReactNode }>) {
  return <UsersApiContext value={api}>{children}</UsersApiContext>;
}

/** Returns the injected UsersApi. Throws if no provider is mounted. */
export function useUsersApi(): UsersApi {
  const api = useContext(UsersApiContext);
  if (!api) throw new Error('useUsersApi must be used within UsersApiProvider');
  return api;
}
```

```ts
// features/users/api/use-users-query.ts
import { useQuery } from '@tanstack/react-query';
import { useUsersApi } from '@/shared/api/api-context';

export const userKeys = { all: ['users'] as const };

/** Fetches the user list. */
export function useUsersQuery() {
  const api = useUsersApi();
  return useQuery({ queryKey: userKeys.all, queryFn: ({ signal }) => api.list(signal) });
}
```

```tsx
// features/users/components/user-list.tsx
import { useUsersQuery } from '../api/use-users-query';

/** Renders the user list with loading, error and empty states. */
export function UserList() {
  const { data, isPending, isError, error, refetch } = useUsersQuery();

  if (isPending) return <p role="status">Loading users…</p>;
  if (isError)
    return (
      <div role="alert">
        <p>Failed to load users: {error.message}</p>
        <button type="button" onClick={() => void refetch()}>Retry</button>
      </div>
    );
  if (data.length === 0) return <p>No users yet.</p>;

  return (
    <ul aria-label="Users">
      {data.map((u) => (
        <li key={u.id}>{u.name} — {u.email}</li>
      ))}
    </ul>
  );
}
```

```tsx
// features/users/components/user-list.test.tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';
import { UsersApiProvider, type UsersApi } from '@/shared/api/api-context';
import { UserList } from './user-list';

function renderWith(api: UsersApi) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <UsersApiProvider api={api}>
        <UserList />
      </UsersApiProvider>
    </QueryClientProvider>,
  );
}

describe('UserList', () => {
  it('renders users', async () => {
    renderWith({ list: vi.fn().mockResolvedValue([{ id: '1', name: 'Ada', email: 'ada@x.io' }]) });
    expect(await screen.findByRole('list', { name: 'Users' })).toHaveTextContent('Ada');
  });

  it('shows empty state', async () => {
    renderWith({ list: vi.fn().mockResolvedValue([]) });
    expect(await screen.findByText('No users yet.')).toBeInTheDocument();
  });

  it('shows error and retries', async () => {
    const list = vi.fn()
      .mockRejectedValueOnce(new Error('boom'))
      .mockResolvedValueOnce([{ id: '1', name: 'Ada', email: 'ada@x.io' }]);
    renderWith({ list });

    expect(await screen.findByRole('alert')).toHaveTextContent('boom');
    await userEvent.setup().click(screen.getByRole('button', { name: 'Retry' }));
    expect(await screen.findByText(/Ada/)).toBeInTheDocument();
  });
});
```