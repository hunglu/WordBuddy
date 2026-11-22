#!/bin/bash
# Creates the four WordBuddy databases, one SQL login + database user per service, and gives each
# user db_owner on its own database only (needed for EF Core migrations). Idempotent: safe on every `up`.
# Needs MSSQL_SA_PASSWORD and <SERVICE>_DB_PASSWORD for the four services.
# Passwords must contain only letters and digits (they are placed in SQL text and connection strings).
# CHECK_POLICY = OFF: random hex secrets often lack an upper-case letter and would fail the Windows
# complexity rule. Use long random values (openssl rand -hex 24).
set -eu

SQLCMD=/opt/mssql-tools18/bin/sqlcmd

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

setup WordBuddyIdentity wb_identity "$IDENTITY_DB_PASSWORD"
setup WordBuddyContent  wb_content  "$CONTENT_DB_PASSWORD"
setup WordBuddyQuiz     wb_quiz     "$QUIZ_DB_PASSWORD"
setup WordBuddyProgress wb_progress "$PROGRESS_DB_PASSWORD"
rm -f /tmp/setup.sql
