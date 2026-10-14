# Review: WB-50 Lexeme admin view

PR: #54 · Round 1 · Reviewed commit: 78af871 · 2026-10-14T17:18:03+07:00

## Verdict

Changes requested — 2 majors in deployment config: audio uploads over 1 MB fail behind Nginx, and Identity can crash on startup under docker compose.

The application code is sound. `AdminOnly` covers all 5 endpoints. The guarded asset delete, the `LIKE` escaping, the `409` path and the cache invalidation are correct.

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `WordBuddy.UI/nginx.conf:53` | The `/api/vocabulary` location has no `client_max_body_size`, so the Nginx default of 1 MB applies. Only `/api/media` sets `50m`. The plan says the limit is 5 MB and "Infra: none". **Failure:** in docker compose or kind, an admin uploads a 2 MB MP3 → Nginx answers `413` → UI shows "Could not upload the audio." The integration tests do not catch this because they bypass Nginx. The k8s Ingress (`WordBuddy/k8s/ingress.yaml`) has the same 1 MB ingress-nginx default. That gap already exists for `/api/media`. | Add `client_max_body_size 6m;` to the `/api/vocabulary` location, or to a dedicated `location ^~ /api/vocabulary/admin/lexemes`. For kind, add `nginx.ingress.kubernetes.io/proxy-body-size` to the Ingress, or record it as a known gap. |
| 2 | major | `WordBuddy/docker-compose.yml:81` | `SupportLinks__UnlinkOverrideWaitDays: ${SupportLinks__UnlinkOverrideWaitDays}` has no default, and `.env.example` does not list the variable. If `.env` lacks it, compose passes an empty string. Verified on net10.0: binding `""` to `int` throws `Failed to convert configuration value '' at 'SupportLinks:UnlinkOverrideWaitDays' to type 'System.Int32'`. `Get<SupportLinkOptions>()` runs in `AddInfrastructure`. **Failure:** a fresh clone runs `make up` → identity-api crashes at startup. | Use `${SupportLinks__UnlinkOverrideWaitDays:-7}`, which matches `appsettings.json`. Add the variable to `WordBuddy/.env.example`. |
| 3 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.Infrastructure/Services/LocalFileStorageService.cs:63` | The path-traversal guard compares with `OrdinalIgnoreCase`. On Linux, paths are case-sensitive, so `/app/MEDIA/x` passes for base `/app/media/`. Today's input is low-risk, because `Url` is server-written. | Use `Ordinal` on non-Windows, for example `OperatingSystem.IsWindows() ? OrdinalIgnoreCase : Ordinal`. |
| 4 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.Application/Features/LexemesAdmin/LexemeAudioCleanup.cs:21` | The `IsUrlUsedAsync` check and the file delete run after the commit. A concurrent upload for the same lexeme and locale can interleave: it writes the file, Remove deletes the file, then Upload inserts the row. The result is a row whose file is missing, and `GET /api/media/{id}` returns `500`. This needs two admins on one lexeme at the same moment. | Accept, as with last-write-wins, or note it in the living spec. |
| 5 | nit | `WordBuddy.UI/src/components/admin-lexemes/LexemeAudioPanel.tsx:33` | Some browsers and OSes report MP3 files as `audio/mp3`. The UI then rejects a valid file. The server rejects it too, because multipart sends `file.type`. | Accept `audio/mp3` as an alias in the UI and the validator, and normalize it to `audio/mpeg`. Or document the behaviour. |
| 6 | nit | `WordBuddy.UI/src/components/admin-lexemes/LexemeForm.tsx:68` | `'' as PartOfSpeech` is a type assertion that hides the "no POS" state from the type system. | Type the form field as `PartOfSpeech \| ''` and let Zod narrow it. |
| 7 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.Application/Features/LexemesAdmin/Commands/UploadLexemeAudio/UploadLexemeAudioCommandHandler.cs:86` | The "old asset has a different `Url`" branch is only unit-tested. It is the one path that inserts a new row and runs the guarded delete in the same SQL transaction. | Tester: add one integration test that seeds an old asset with another `Url`, uploads, and asserts the old row and file are gone. |

## Focus checks

| Check | Result |
|---|---|
| `AdminOnly` on every endpoint | Class-level `[Authorize(Policy = "AdminOnly")]`. The theory covers all 5 routes: anonymous → 401, Adult and Child → 403. |
| 415 before 403 on audio PUT | `IFormFile` adds a `Consumes` constraint that runs during endpoint selection. It reveals nothing beyond the route shape. Acceptable. |
| 6 FK columns | Matches the model snapshot exactly: `Lexemes.Uk/UsAudioAssetId`, `Senses.Audio/ImageAssetId`, `DailyPhrases.Audio/VideoAssetId`. No other table, and no other service, holds a `MediaAsset` id. |
| Transaction order | Unlink (`SaveChanges`) → guarded `DELETE … OUTPUT` → commit → file. No retrying execution strategy is configured, so no replay issue. |
| SQL 547 | Caught per statement. With `XACT_ABORT OFF` (the SqlClient default), the transaction stays usable and the unlink commits. |
| Path traversal | `GetFullPath` plus a base-prefix check with a trailing separator. Rooted and `..` inputs are refused. See nit 3. |
| `LIKE` / injection | The term is normalized, then `\ % _ [` are escaped with `ESCAPE '\'`. Always a parameter. The raw SQL uses a `DbParameter`. |
| 409 | The pre-check plus the `2601`/`2627` mapping on the index name are both present. |
| Cache | Autofill catalog key plus both shared-pool keys, after every successful command. `content:sense:{id}` is never set, and the media GET is not cached. |
| Logs | Ids, locale, POS and counts only. The storage log has the relative path (`audio/lexeme-{id}-{locale}.mp3`), with no user data. |
| UI conventions | TanStack Query only, `isError` handled on both pages, `wb-` tokens only, Framer Motion page entry, no `any` or `enum`. |
| Line endings | `git diff` and `--ignore-cr-at-eol` both give 4593 lines. 3 edited files stay CRLF, as on `main`. New files are LF. |

## Plan conformance

- All 48 tasks are reflected in the diff.
- Reported deviations are acceptable:
  - asset ids in `AdminLexemeDetailDto` (needed for `/api/media/{id}`);
  - nullable `PartOfSpeech` in the request (missing POS → 400);
  - 415 before 403 on the audio PUT.
- E2E API and UI tests are written but not run. `/test` must run them.
- Sam-requested extras (not scope creep):
  - `docker-compose.yml` env var — see finding 2;
  - `@types/node` in `e2e/ui` — OK;
  - placeholder `docs/features/lexeme-admin.md` — OK, `/test` fills it.
- No EF migration, so no database diagram update is needed.

## Fix round 1 — response

| # | Status | Fix |
|---|---|---|
| 1 | Fixed | `nginx.conf`: `client_max_body_size 6m;` in `/api/vocabulary`. `k8s/ingress.yaml`: annotation `proxy-body-size: 50m` with a comment (one Ingress, so it covers all paths and `/api/media`). Not applied to a cluster. |
| 2 | Fixed | `docker-compose.yml`: default `:-7` (matches `appsettings.json` and `SupportLinkOptions`). `.env.example` lists the variable with a comment. `.env` not read. |
| 3 | Fixed | `LocalFileStorageService`: `Ordinal` on Linux, `OrdinalIgnoreCase` on Windows. Existing traversal test kept (it runs on Windows here, so the Linux branch is not run). |
| 4 | Deferred (accepted) | Needs two admins on one lexeme and locale at the same moment. A safe fix needs a lock or a retry. Last write wins, as elsewhere. Recovery: upload again. |
| 5 | Fixed | UI accepts `audio/mp3` and `audio/mpeg`. The validator accepts the alias too; the file is stored and served as `audio/mpeg`. New integration test for the alias. |
| 6 | Fixed | `LexemeForm`: the Zod field is `'' \| PartOfSpeech` as input and a transform rejects `''`, so the output is a real `PartOfSpeech`. Cast removed. |
| 7 | Fixed | New integration test: old asset with another `Url` → upload → old row and old file gone, new row linked. |

Checks: Content build 0 errors; unit 415 pass; integration 184 pass; `npm run build` OK. nginx syntax not checked (no nginx or docker used).
