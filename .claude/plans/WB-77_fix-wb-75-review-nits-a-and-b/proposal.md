---
title: WB-77_Fix WB-75 review nits A and B
status: reviewed
type: bugfix
issue: 77
pr: 78
affects: docs/features/WB-75_production-deployment-single-host-docker-compose-and-kubernetes.md
supersedes: none
version: 1.4
created: 2026-11-22T21:40:15+07:00
updated: 2026-11-22T22:34:35+07:00
---

## Problem

Two open nits from the WB-75 review round 2 (PR #76):

- **A — PII in migration logs (SQL Server).** `MigrationFailureLog` (Identity, Content, Quiz, Progress) logs the provider message for a `DbException`. A SQL Server duplicate-key error (2601 / 2627) puts the key value in that message. Example: a user registers with the email later set as `BOOTSTRAP_ADMIN_EMAIL`; the admin bootstrap insert fails and the email reaches the log. Postgres is not affected.
- **B — unchecked passwords in `deploy/single-host/sqlserver/init.sh`.** Passwords go into SQL as raw `sqlcmd` variables. A `'` breaks the batch or runs extra SQL; a missing `.env` value creates a login with a blank password.

## Goal

- No user data (emails, key values) in migration failure logs on either provider.
- The SQL Server init step refuses unsafe or empty passwords before it runs any SQL.

## Target users

Both. Child and adult accounts are protected the same way: no PII in logs.

## Success criteria

- A duplicate-key failure during `migrate` logs the error number and stack trace, but not the key value. A test proves it (SQL Server).
- The Identity bootstrap does not hit the duplicate-key path when a user with the admin email already exists (it fails with a clear, PII-free error instead).
- `init.sh` exits non-zero with a clear message when a password is empty or does not match `^[A-Za-z0-9]{16,}$`; the overlay uses `${VAR:?}` so Compose fails fast on a missing value.
- Existing Postgres and SQL Server suites stay green.

## Constraints

- Bugfix on the WB-75 deployment feature; no new deployment features.
- Keep the 4 `MigrationFailureLog` copies identical apart from the service name (ADR 0001: no shared project reference).
- Out of scope: nit C (docs wording), the login/register rate limit, CI checks for deploy files.
