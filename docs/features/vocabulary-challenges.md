---
feature: Vocabulary challenges
services: Progress, UI
audience: Both
state: proposed
last-updated-by: WB-29_vocabulary-challenges
---

# Vocabulary challenges

_Proposed — not on main yet._

## What it does

_Proposed — not on main yet._ System-created ranked and target challenges scored from ReviewLog; restricted subset for children.

## Rules

_To be defined in `/plan`._

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

_To be defined in `/plan`._

## Pending changes

- `WB-29_vocabulary-challenges` — System-created ranked and target challenges scored from ReviewLog; restricted subset for children. (#29)

### Notes for the spec (review round 1, to merge into Rules by `/test`)

- A guardian switching challenges off for a child removes the consent of every guardian of that child. Safe for privacy; a second guardian must switch it on again.
- A revoked support link removes the consent that supporter gave; the cleanup also runs on a replayed revoke, so a failed first attempt is repaired.
- The cached board snapshot in Redis holds a `UserId` per row. It is never sent in a DTO and expires with the cache entry.
- Listening Sprint counts correct due listening answers per learner, with no daily cap. Only MemoryPoints has a daily cap.

## Change history

None.
