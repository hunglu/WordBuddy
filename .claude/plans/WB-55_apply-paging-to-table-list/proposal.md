---
title: WB-55_Apply paging to table/list
status: needs-fixes
type: new
issue: 55
pr: 57
affects: docs/features/WB-55_apply-paging-to-table-list.md
supersedes: none
version: 3.0
created: 2026-10-14T21:29:15+07:00
updated: 2026-10-15T09:35:29+07:00
---

## Problem

- Most tables and lists load every row in one call.
- My Shared Pool also display a long list of cards.
- Long lists load slowly and are hard to scan.
- Only the admin lexemes list pages today, so paging looks and behaves differently across the app.

## Goal

Every table and list in the app uses **server-side** paging, with one shared pager component and
one API paging shape.

```
UI list → GET ...?page=N&pageSize=M → { items, page, pageSize, totalCount } → shared <Pager>
```

Scope — every list in the app, including:

| Area | Lists |
| --- | --- |
| Admin | Lessons, lexemes (align to shared shape), support links, vocabulary moderation |
| Learner vocabulary | Vocabulary builder list, shared pool, group words |
| Groups & challenges | Groups, group members, challenges, challenge board |

## Target users

Both — children and adults. Admin lists apply to admins only.

## Success criteria

- Every list endpoint accepts `page` / `pageSize` and returns items plus total count.
- Every table/list in the UI shows the same pager (previous / next, page number, total).
- No page fetches all rows of a long list.
- Child and adult accounts see the same pager; child content restrictions still apply per page.

## Constraints

_Not specified._
