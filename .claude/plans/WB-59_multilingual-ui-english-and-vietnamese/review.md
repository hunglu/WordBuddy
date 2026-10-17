# Review: Multilingual UI (English and Vietnamese)

PR: #60 · Round 1 · Reviewed commit: d51293a · 2026-10-17T16:07:06+07:00

## Verdict

Changes requested — one major: after a failed save, a page reload reverts the UI to the old profile language.

Checks run: `npm run build` passes (key parity and typed keys hold). Identity language tests pass (25 unit, 8 integration).

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `WordBuddy.UI/src/hooks/useLanguageSync.ts:14-20` | The effect runs on mount with the **persisted** user, so the profile language wins on every app load, not only on login. After a failed save, the persisted user still holds the old value. Scenario: a new account is registered in `en` (RegisterPage always sends a language) → switch to `vi` → PUT fails → the UI says "changed on this device, but not saved" → reload → the UI is back in English. The localStorage choice and the message are both wrong. | Apply the profile language only on a real login transition. Keep the first hydrated `userId` in a `useRef` and skip that run. Alternative: call `applyLanguage(auth.user.preferredLanguage)` in the Login/Register `onSuccess`, where the user is fresh. |
| 2 | nit | `WordBuddy.UI/src/components/LanguageSwitcher.tsx:22-25` | Fast switching (`en → vi → en`) sends overlapping PUTs. They can finish out of order, so the profile can end on `vi` while the UI shows `en`. | Give the mutation `scope: { id: 'preferred-language' }` (TanStack v5 runs it serially). |
| 3 | nit | `WordBuddy.UI/src/hooks/useLanguageSync.ts:15` | A language picked on `/login` is not saved when the profile has no language (`null`). The next device falls back to the browser language. | When logged in and `preferredLanguage` is `null`, save the current language once. |
| 4 | nit | `WordBuddy.UI/src/types/index.ts:58` | `preferredLanguage: string \| null` is required, but sessions persisted before this change have no field (`undefined`). The comment says so, the type does not. | `preferredLanguage?: string \| null`, like `dateOfBirth`. |
| 5 | nit | `WordBuddy.UI/src/components/autofill/SenseDetails.tsx:43,63` | `sense.partOfSpeech` shows the raw enum (`Noun`) on the learner autofill card, also for children. Admin pages translate it with `admin:pos`. | Render `t(\`admin:pos.${sense.partOfSpeech}\`)`, or move `pos` to `common`. |
| 6 | nit | `WordBuddy/src/Services/Identity/README.md:16` | README says a malformed code returns `400 User.InvalidLanguage`. The validator runs first, so the real title is `UpdatePreferredLanguage.Validation` (`RegisterUser.Validation` on register). The seeded `errors:codes.User.InvalidLanguage` is never reached; `vi` users get the generic fallback. | Fix the README text. Optionally seed `UpdatePreferredLanguage.Validation` in `errors:codes`. |
| 7 | nit | `WordBuddy.UI/src/App.tsx`, `pages/VocabularyBuilderPage.tsx`, `pages/AdminLessonEditorPage.tsx` | These files changed from CRLF to LF. The diff shows 229 / 608 / 372 lines; the real change is 3 / 62 / 76. Review and blame get noisy. | Restore the original line endings, or add a `.gitattributes` rule in a separate change. |
| 8 | nit | `WordBuddy.UI/src/pages/LoginPage.tsx:26` (same pattern in all `makeXSchema` forms) | The schemas rebuild correctly on a language change (`t` identity changes, RHF reads the new resolver). Error messages already on screen keep the old language until the field validates again. | Accept, or call `trigger()` for fields with errors on `languageChanged`. |
| 9 | nit | `WordBuddy.UI/src/i18n/locales/vi/challenges.ts:28` | `'{{metric}}: nhiều nhất thắng'` reads awkwardly, and children see it. | `'{{metric}}: ai cao nhất sẽ thắng'`. |

Verified, no finding:

- **Backend:** format-only check, normalised to lower case, `null` clears. Own account only (`[Authorize]`, id from the token). Logs carry the user id and error code, never the value. No event and no cache, as planned.
- **Migration:** adds one nullable `nvarchar(10)` column and nothing else. Snapshot and `docs/database-diagram/identity.md` match.
- **Child vs adult:** same endpoint and same switcher. The integration test covers both age groups.
- **Typed keys:** `vi` is typed against `en` through `TranslationOf`. `CustomTypeOptions` types `t()`. No `any`, no `enum`, no casts.
- **Hard-coded strings:** a grep of JSX text and `placeholder|aria-label|title|alt` leaves only comments. Untouched components (`SenseCard`, `GrammarRuleList`, `ChoiceOptions`, `PersonalContextNote`) have no UI text.
- **`problemMessage`:** nested dotted codes resolve through i18next `deepFind`. Unmapped codes in `vi` fall back to translated text.
- **Vietnamese copy:** the text is natural and short. Child screens (nav, review, gate, dashboard) use simple words.

## Plan conformance

- All `tasks.md` items are reflected in the diff. E2E tasks moved to `/test`, as the plan says.
- ADR `docs/adr/0006-ui-i18n.md` is added. The `CLAUDE.md` rule is proposed, not edited.
- Out of scope: the line-ending churn (finding 7) only.
