---
title: 2 navigation Items have selected state
status: reviewed
type: bugfix
issue: 5
pr: 7
affects: docs/features/app-navigation.md
supersedes: none
version: 1.7
created: 2026-10-01T16:50:32+07:00
updated: 2026-10-01T17:17:39+07:00
---

## Problem

In the sidebar, two navigation items appear selected at the same time. Steps to reproduce:

1. Log in to the website.
2. Select **My Vocabulary** in the side navigation.
3. Select **Shared Pool** in the side navigation.

Both **My Vocabulary** and **Shared Pool** now show the selected state.

## Goal

Only the navigation item for the current page shows the selected state.

## Target users

Both (children and adults). The sidebar is the same for both age groups.

## Success criteria

- On `/vocabulary/shared`, only **Shared Pool** is selected.
- On `/vocabulary`, only **My Vocabulary** is selected.
- Every other sidebar item still shows the selected state correctly on its own route.

## Constraints

_Not specified._
