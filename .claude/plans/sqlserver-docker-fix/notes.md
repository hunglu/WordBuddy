---
status: done
date: 2026-09-26
---

# Fix: `sqlserver` container in docker-compose crashes on startup

## Symptom

`docker compose up` — `sqlserver` never becomes healthy; logs are full of
`find: '/proc/13/...': Permission denied`.

## Diagnosis

- The `Permission denied` lines are noise from SQL Server's crash dumper (paldumper) running
  *after* the crash — not the cause.
- Crash dumps in the `wordbuddy_sqlserver-data` volume showed three layered problems:
  1. `Unable to set persistent hive root` (errno 13) — `/var/opt/mssql/.system/system/security.hiv`
     was owned by `root:root 640` (written on Jun 29 by a root-run container); SQL Server runs
     as `mssql` (uid 10001).
  2. After fixing ownership: `The database 'master' cannot be opened because it is version 998.
     This server supports version 958 and earlier.` — the volume was created on Jun 7 by
     **SQL Server 2025** (errorlog header: 17.0.4015.4), but compose pins `2022-latest`.
     A downgrade is not possible.
  3. With the 2025 image: `[AppLoader] Failed to load LSA: 0xc0070102` — `security.hiv` had been
     rewritten by the failed 2022 starts.
- A fresh volume starts fine on `2022-latest`, so the image and host are OK.

## Fix applied

- `chown -R 10001:10001` on `.system`, `data`, `log`, `secrets` inside the volume.
- Moved the corrupted hive aside: `.system/system/security.hiv.bak-20260926` (2025 regenerated it).
- `WordBuddy/docker-compose.yml`: `sqlserver` and `sqlserver-init` → `mssql/server:2025-latest`.
- Verified: SQL Server 2025 on the existing volume reaches "ready for client connections".

## Follow-up fix (2026-09-27): `Login failed for user 'sa'`

- Cause: the volume kept the SA password from Jun 7; `SA_PASSWORD` in `.env` only applies on first init.
- Fix: `docker compose stop sqlserver`, then reset the password to the `.env` value without exposing it:
  `docker compose run --rm --no-deps -T --entrypoint bash sqlserver -c 'MSSQL_SA_PASSWORD="$SA_PASSWORD" /opt/mssql/bin/mssql-conf -n set-sa-password'`,
  then `docker compose start sqlserver`.
- Verified: `sqlserver` reports `healthy` (healthcheck logs in as `sa` with the `.env` password).
- Still to check: the `*_CONNECTION_STRING` values in `.env` must use the same password.

## Follow-ups

- `k8s/sqlserver-statefulset.yaml` and `k8s/sqlserver-init-job.yaml` still use `2022-latest`
  (separate PVC, so they aren't affected, but the versions now differ).
- The SA password stored in the volume is whatever it was on Jun 7; `SA_PASSWORD` only applies
  on first init. If `.env` differs, the healthcheck fails — rotate it with `ALTER LOGIN sa` or
  recreate the volume.
- The volume only held an old single `WordBuddy` DB; if nothing in it is needed, the alternative
  is `docker volume rm wordbuddy_sqlserver-data` and staying on 2022.
