# WordBuddy E2E UI tests

Playwright + [playwright-bdd](https://vitalets.github.io/playwright-bdd/) end-to-end tests
driving the real WordBuddy.UI in a browser. Gherkin scenarios live in `features/*.feature`, step
definitions in `steps/*.ts`.

## One-time setup

```bash
cd e2e/ui
npm install
npx playwright install   # downloads browser binaries — run this yourself, it's not automated
```

## Prerequisites to run

The UI and backend services must be running, e.g. `make up` (or `docker compose up --build`)
from `WordBuddy/`, which serves the UI at `http://localhost:3000`. A test account matching
`E2E_TEST_EMAIL`/`E2E_TEST_PASSWORD` (see below) must exist in that environment's Identity
service.

## Running

```bash
npm test              # headless
npm run test:headed   # with a visible browser, useful while writing new scenarios
npm run report        # open the last HTML report
```

## Configuration

| Env var | Purpose | Default |
|---|---|---|
| `UI_BASE_URL` | Base URL of the UI under test | `http://localhost:3000` |
| `E2E_TEST_EMAIL` | Login credential used by `steps/login.steps.ts` | `learner@example.com` |
| `E2E_TEST_PASSWORD` | Login credential used by `steps/login.steps.ts` | `ChangeMe123!` |
| `E2E_ADMIN_EMAIL` | Admin login used by `steps/navigation.steps.ts` | `admin@wordbuddy.com` (Identity dev seed) |
| `E2E_ADMIN_PASSWORD` | Admin login used by `steps/navigation.steps.ts` | `Admin@123` (Identity dev seed) |

Never commit real test credentials — set them as environment variables in CI/local shell instead.

## Adding a new scenario

1. Add a `.feature` file under `features/` describing the behavior in Gherkin.
2. Add matching step definitions under `steps/` using `createBdd()` from `playwright-bdd`
   (follow `steps/login.steps.ts` as the pattern).
3. `playwright-bdd` generates runnable Playwright specs from these on `npm test`
   (via `bddgen`) — no separate registration step needed.
