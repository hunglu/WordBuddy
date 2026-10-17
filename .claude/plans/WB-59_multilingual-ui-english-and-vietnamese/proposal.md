---
title: WB-59_Multilingual UI (English and Vietnamese)
status: done
type: new
issue: 59
pr: 60
affects: docs/features/WB-59_multilingual-ui-english-and-vietnamese.md, docs/features/user-profile.md
supersedes: none
version: 12.0
created: 2026-10-17T11:25:26+07:00
updated: 2026-10-17T19:30:00+07:00
---

## Problem

All UI text is English only. Vietnamese learners, especially beginners and children, cannot read
menus, buttons and messages easily.

## Goal

The UI is available in English and Vietnamese. A learner picks a language with a switcher; the
choice is saved to their profile.

```
browser language ─► UI before login
switcher ─► UI changes at once ─► saved to profile ─► restored on next login (any device)
```

## Target users

Both. Children and adults get the same language options and the same switcher.

## Success criteria

- Every UI string comes from a translation file (`en`, `vi`); no hard-coded text.
- A language switcher changes the UI at once, without a page reload.
- The choice is saved to the profile and restored after login on any device.
- Before login, the UI uses the browser language (`vi` → Vietnamese, else English).
- Adding a third language later needs only a new translation file plus one config entry.

## Constraints

- In scope: UI text only (menus, buttons, labels, validation and error messages).
- Out of scope: translating lesson content (vocabulary, grammar, daily phrases, quizzes),
  notifications and emails.
- Out of scope: more than two languages now — but the design must make adding one easy.
