---
feature: <Feature name>
services: <Identity | Content | Quiz | Progress | Notification | UI>
audience: <Child | Adult | Both>
state: <proposed | shipped>
last-updated-by: <slug>
---

# <Feature name>

<One line: what the feature does for the learner.>

## What it does

<Behaviour on `main` today — user-visible, not implementation history. Add a diagram for any flow
with 3+ steps or components.>

## Rules

<Business rules as short bullets. Always include a **Child vs adult** bullet.>

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

<Routes / pages involved.>

## Pending changes

<Unmerged proposals that will change this feature — added by `/propose`, removed by `/test` on merge.>

- `<slug>` — <one-line goal> (#<issue>)
  - ADDED: <new rule / endpoint / screen>
  - MODIFIED: <existing rule> → <new behaviour>
  - REMOVED: <rule / endpoint that goes away>

(Keep only the delta lines that apply. `/test` folds them into the sections above on merge.)

## Change history

- `<slug>` — <one line> (#<issue>)
