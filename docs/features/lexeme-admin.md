---
feature: Lexeme admin
services: Content | UI
audience: Adult
state: shipped
last-updated-by: WB-55_apply-paging-to-table-list
---

# Lexeme admin

Admins search lexemes, read their catalog senses, correct dictionary fields and manage UK/US audio.

## What it does

- Admin lists lexemes (paged, "contains" search, POS and CEFR filters).
- Admin opens one lexeme: fields form, audio panel, read-only catalog senses.
- Admin corrects POS, IPA (UK/US), syllables, word forms, CEFR and frequency rank.
- Admin uploads or removes UK (`en-GB`) and US (`en-US`) MP3 audio.
- `Lemma` is not editable (rename = merge/split, out of scope).

## Rules

- **Child vs adult:** admin only. Every endpoint has `[Authorize(Policy = "AdminOnly")]`: anonymous → 401, adult and child learner → 403. UI redirects non-admins to `/`; the nav item shows for admins only.
- **Privacy:** the detail view shows full text for catalog senses (System or Shared) only. Private, pending and rejected learner senses appear as a count. The lemma shown is the catalog spelling (ADR 0004).
- **Search:** "contains" on the normalized lemma, with `\ % _ [` escaped; prefix matches rank first. Scans the unique index; no new index. Revisit past about 100 000 lexemes.
- **Edit:** full replace of the editable fields. POS is required. Too-long values give 400, never truncation. Word forms: trimmed, de-duplicated, max 20.
- **409 clash:** changing POS to one that already exists for the same lemma returns 409 `Lexeme.DuplicatePartOfSpeech` (pre-check plus SQL 2601/2627 safety net).
- **Audio upload:** `audio/mpeg` (alias `audio/mp3`), 1 byte to 5 MB. Stored as `audio/lexeme-{id}-{locale}.mp3`; overwrites in place, so no orphan.
- **Guarded audio delete:** `DELETE` unlinks the audio. The `MediaAsset` row and the file are removed only when none of the six FK columns (`Lexemes` Uk/Us, `Senses` audio/image, `DailyPhrases` audio/video) and no other row with the same `Url` use it. A file-delete failure is logged and still returns 204. `DELETE` with no audio returns 204.
- **Body limits (every hop):** UI Nginx `/api/vocabulary` 6m; controller `[RequestSizeLimit]` 6 MB; k8s Ingress `proxy-body-size` 50m (applies to all paths). A body over 6 MB gets an Nginx 413.
- **Cache:** after each command, the autofill catalog key and both shared-pool keys are deleted. Admin queries are not cached.
- **Concurrency:** last write wins (no row version). A remove racing an upload on the same lexeme and locale can leave a row whose file is missing; accepted risk, recovery = upload again.

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|
| GET | `/api/vocabulary/admin/lexemes?search=&partOfSpeech=&cefrLevel=&page=&pageSize=` | Content | `AdminOnly` |
| GET | `/api/vocabulary/admin/lexemes/{id}` | Content | `AdminOnly` |
| PUT | `/api/vocabulary/admin/lexemes/{id}` (204; 400, 404, 409) | Content | `AdminOnly` |
| PUT | `/api/vocabulary/admin/lexemes/{id}/audio/{locale}` (multipart `file`; 200 `MediaAssetDto`) | Content | `AdminOnly` |
| DELETE | `/api/vocabulary/admin/lexemes/{id}/audio/{locale}` (204) | Content | `AdminOnly` |

`{locale}` is `en-GB` or `en-US`. `pageSize` 1 to 100 (default 25).

## UI

| Route | Page |
|---|---|
| `/admin/lexemes` | Search box (300 ms debounce), POS and CEFR selects, table, Prev/Next pager. Loading, error and empty states. |
| `/admin/lexemes/:id` | Header (lemma, POS badge), fields form (React Hook Form + Zod, word forms one per line, shows 409 detail), audio panel (play, upload, remove with confirm), senses panel (read-only, "N private learner senses (not shown)"). |

## Known gaps

- Linux branch of the `LocalFileStorageService` path guard is untested (tests ran on Windows).
- Ingress 50m limit applies to all paths, including anonymous `/api/auth`; split the Ingress before a cloud deploy.

## Paging (WB-55)

`GET /api/vocabulary/admin/lexemes` now uses the shared `PagedResult` type from `WordBuddy.Shared.Kernel` 1.1.0. The JSON is unchanged. The page uses the shared `Pager` component.

## Pending changes

## Change history

- `WB-50_lexeme-admin-view` — admin lexeme list, edit and audio management (#50)
- `WB-55_apply-paging-to-table-list` (#55, PR #57) — lexeme list moved to the shared paged type and pager
