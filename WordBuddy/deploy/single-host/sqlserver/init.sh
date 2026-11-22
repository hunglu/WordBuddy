#!/bin/bash
# Creates the four WordBuddy databases, one SQL login + database user per service, and gives each
# user db_owner on its own database only (needed for EF Core migrations). Idempotent: safe on every `up`.
# Needs MSSQL_SA_PASSWORD and <SERVICE>_DB_PASSWORD for the four services.
# Every password must be 16+ letters and digits only (they are placed in SQL text and connection strings).
# The script checks all five before it runs any SQL and exits 1 on the first bad one (value never printed).
# The service logins use CHECK_POLICY = OFF, so hex secrets are fine for them. The sa login still needs
# the SQL Server complexity rule (3 of 4: upper, lower, digit, symbol): use mixed case for MSSQL_SA_PASSWORD.
set -eu

SQLCMD=/opt/mssql-tools18/bin/sqlcmd

require_password() {
  name="$1"
  value="${!name:-}"
  if [ -z "$value" ]; then
    echo "init: $name is empty" >&2
    exit 1
  fi
  if ! [[ "$value" =~ ^[A-Za-z0-9]{16,}$ ]]; then
    echo "init: $name must be 16+ letters and digits" >&2
    exit 1
  fi
}

setup() {
  db="$1"; login="$2"; password="$3"
  cat > /tmp/setup.sql <<'SQL'
IF DB_ID(N'$(db)') IS NULL CREATE DATABASE [$(db)];
IF SUSER_ID(N'$(login)') IS NULL
  CREATE LOGIN [$(login)] WITH PASSWORD = N'$(pw)', CHECK_POLICY = OFF;
ELSE
  ALTER LOGIN [$(login)] WITH PASSWORD = N'$(pw)';
GO
USE [$(db)];
IF DATABASE_PRINCIPAL_ID(N'$(login)') IS NULL CREATE USER [$(login)] FOR LOGIN [$(login)];
ALTER ROLE db_owner ADD MEMBER [$(login)];
GO
SQL
  "$SQLCMD" -S sqlserver -U sa -P "$MSSQL_SA_PASSWORD" -C -b -v db="$db" login="$login" pw="$password" -i /tmp/setup.sql
  echo "ready: $db"
}

for name in MSSQL_SA_PASSWORD IDENTITY_DB_PASSWORD CONTENT_DB_PASSWORD QUIZ_DB_PASSWORD PROGRESS_DB_PASSWORD; do
  require_password "$name"
done

setup WordBuddyIdentity wb_identity "$IDENTITY_DB_PASSWORD"
setup WordBuddyContent  wb_content  "$CONTENT_DB_PASSWORD"
setup WordBuddyQuiz     wb_quiz     "$QUIZ_DB_PASSWORD"
setup WordBuddyProgress wb_progress "$PROGRESS_DB_PASSWORD"
rm -f /tmp/setup.sql
