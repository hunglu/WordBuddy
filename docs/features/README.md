# Living feature specs

One file per feature, copied from `_template.md`. The `tester` agent updates the file(s) named in a
proposal's `affects:` field when it merges that proposal, so these always match `main`.

| Feature | Spec | Status |
|---|---|---|
| Auth (register/login/refresh) | _to backfill_ | shipped |
| Lessons, vocabulary, grammar, daily phrases | _to backfill_ | shipped |
| Quiz | _to backfill_ | shipped |
| Progress | _to backfill_ | shipped |
| Vocabulary builder & check-up | `vocabulary-builder.md` (created on merge) | blocked |
| UI theme tokens | `ui-theme.md` | shipped |
| App navigation (sidebar) | `app-navigation.md` | proposed |

Backfilling a shipped feature: ask Claude "backfill docs/features/<name>.md from the code" —
read-only against code, writes only this folder.
