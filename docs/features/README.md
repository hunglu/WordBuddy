# Living feature specs

One file per feature, copied from `_template.md`. On merge, the `tester` agent updates every file
named in the proposal's `affects:` field, so these specs always match `main`.

| Feature | Spec | State |
|---|---|---|
| Auth (register / login / refresh) | _to backfill_ | shipped |
| Lessons, vocabulary, grammar, daily phrases | _to backfill_ | shipped |
| Quiz | _to backfill_ | shipped |
| Progress | _to backfill_ | shipped |
| Vocabulary builder & check-up | `vocabulary-builder.md` | shipped |
| Vocabulary storage | `vocabulary.md` | shipped |
| UI theme tokens | `ui-theme.md` | shipped |
| App navigation (sidebar) | `app-navigation.md` | shipped |
| Messaging | `messaging.md` | shipped |
| Learner support links | `learner-support-links.md` | shipped |
| Vocabulary autofill | `vocabulary-autofill.md` | shipped |
| Supporter dashboard | `supporter-dashboard.md` | shipped |
| Learning groups | `learning-groups.md` | shipped |
| Vocabulary challenges | `vocabulary-challenges.md` | shipped |
| User profile | `user-profile.md` | shipped |
| Lesson authoring | `lesson-authoring.md` | shipped |
| Lexeme admin | `lexeme-admin.md` | shipped |
| List paging | `WB-55_apply-paging-to-table-list.md` | shipped |
| Moderator role | `WB-56_moderator-role-to-assist-admin.md` | shipped |
| Multilingual UI (en/vi) | `WB-59_multilingual-ui-english-and-vietnamese.md` | proposed |

**Backfill a shipped feature:** ask Claude `backfill docs/features/<name>.md from the code`.
The task is read-only against code and writes only to this folder.
