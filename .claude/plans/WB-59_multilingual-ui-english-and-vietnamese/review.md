# Review: Multilingual UI (English and Vietnamese)

PR: #60 · Round 2 · Reviewed commit: 6dd36ce · 2026-10-17T17:25:22+07:00

## Verdict

Changes requested — one major: the new auto-save runs on a restored session too, so a stale persisted user can overwrite a language chosen on another device.

Checks run: `npm run build` passes at `6dd36ce`. The CRLF churn is gone (`--ignore-cr-at-eol` gives the same stat as the plain diff).

## Round 1 findings — status

| # | Round 1 finding | Status | Evidence |
|---|---|---|---|
| 1 | Reload reverts a failed save | Resolved | `useLanguageSync.ts:18,27` — the restored user id is kept in a ref; the profile language is applied only when the id differs |
| 2 | Overlapping PUTs | Resolved | `useProfile.ts:40` — `scope: { id: 'preferred-language' }` |
| 3 | No save when profile has no language | Resolved, but see new finding 1 | `useLanguageSync.ts:30-31` |
| 4 | `preferredLanguage` should be optional | Resolved | `types/index.ts:58` |
| 5 | Raw part of speech on autofill card | Resolved | `SenseDetails.tsx:43,63` use `admin:pos.*` |
| 6 | README error code | Resolved | README says `UpdatePreferredLanguage.Validation`; code seeded in `en`/`vi` `errors.ts` |
| 7 | CRLF → LF churn | Resolved | `App.tsx` 3 lines, `VocabularyBuilderPage.tsx` 65, `AdminLessonEditorPage.tsx` 81 vs `main` |
| 8 | Form errors keep old language | Resolved | `useRetranslateFormErrors` in all 11 forms that use `zodResolver` |
| 9 | Awkward `goalRanked` copy | Resolved | `vi/challenges.ts:28` |

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `WordBuddy.UI/src/hooks/useLanguageSync.ts:25-31` | The auto-save also runs for a **restored** session. The persisted user is never refreshed from `/me`, so `preferredLanguage` can be stale (`undefined` in every session persisted before this release, or `null` after a failed save). Scenario: a learner is logged in on phone and laptop before release → after release, picks `vi` on the laptop (saved) → opens the phone, browser in English → restored user has no language → phone saves `en` → the explicit `vi` choice is lost; the next laptop login switches to English. | Auto-save only on a real login, where the user comes fresh from the login reply: `else if (restoredUserId.current !== userId && isSupportedLanguage(i18n.language))`. This also removes the duplicate PUT that React `StrictMode` sends in dev on mount. |
| 2 | nit | `WordBuddy.UI/src/hooks/useLanguageSync.ts:30` | An **unsupported** profile code is overwritten, not ignored. Plan: "Unknown or unsupported codes fall back to `en`". Scenario: a third language `fr` ships; a tab still running the old bundle logs in → saves `en` over `fr`. | Auto-save only when `preferredLanguage` is `null`/`undefined`; leave unsupported codes as they are. |

Verified, no finding:

- **Restored session:** ref = persisted id → profile language not applied → device choice kept. Matches round 1 fix.
- **Logout → login same user:** logout sets the ref to `undefined` → the next login applies the profile language.
- **User switch:** a different id never equals the ref → the new user's profile language is applied.
- **No loop:** the save `onSuccess` calls `updateUser`; the id does not change, so the effect (deps `[userId]`) does not rerun. `login(auth)` in date-of-birth and password changes keeps the same id too.
- **Register:** sends `i18n.language`, so the profile is never `null` after register → no extra save.
- **`useRetranslateFormErrors`:** triggers only `Object.keys(errors)`. With a resolver, RHF `trigger(names)` updates only those names, so untouched fields get no error. The first render is skipped (`previousLanguage` ref). RHF sets `control._options = props` during render, so `trigger` in the effect uses the new-language schema. No form uses `setError`/`errors.root`, so no server error is cleared.
- **Sanity pass (whole PR):** round 1 checks still hold (backend, migration + diagram, typed keys, child vs adult). The fix round touches only the UI, the README and the plan folder.

## Plan conformance

- All `tasks.md` items are reflected in the diff. E2E tasks moved to `/test`, as the plan says.
- Fix round stays inside the round 1 findings. No out-of-scope change.

## Previous rounds


**Round 1** · PR: #60 · Reviewed commit: d51293a · 2026-10-17T16:07:06+07:00

### Verdict

Changes requested — one major: after a failed save, a page reload reverts the UI to the old profile language.

Checks run: `npm run build` passes (key parity and typed keys hold). Identity language tests pass (25 unit, 8 integration).

### Findings

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

### Plan conformance

- All `tasks.md` items are reflected in the diff. E2E tasks moved to `/test`, as the plan says.
- ADR `docs/adr/0006-ui-i18n.md` is added. The `CLAUDE.md` rule is proposed, not edited.
- Out of scope: the line-ending churn (finding 7) only.
