#!/bin/sh
# Creates the four WordBuddy databases and one owner role per service. Idempotent.
# Keep identical to WordBuddy/k8s/production/postgres-init.sh (make k8s-prod-render checks it).
# Needs PGHOST / PGUSER / PGPASSWORD (superuser) and <SERVICE>_DB_PASSWORD for the four services.
# Passwords must not contain quotes or whitespace.
set -eu

create() {
  role="$1"; db="$2"; password="$3"
  psql -v ON_ERROR_STOP=1 -v role="$role" -v pw="$password" <<'SQL'
SELECT format('CREATE ROLE %I LOGIN PASSWORD %L', :'role', :'pw')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'role') \gexec
SELECT format('ALTER ROLE %I PASSWORD %L', :'role', :'pw') \gexec
SQL
  psql -v ON_ERROR_STOP=1 -v role="$role" -v db="$db" <<'SQL'
SELECT format('CREATE DATABASE %I OWNER %I', :'db', :'role')
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = :'db') \gexec
SELECT format('REVOKE ALL ON DATABASE %I FROM PUBLIC', :'db') \gexec
SELECT format('GRANT CONNECT ON DATABASE %I TO %I', :'db', :'role') \gexec
SQL
  echo "ready: $db"
}

create wb_identity WordBuddyIdentity "$IDENTITY_DB_PASSWORD"
create wb_content  WordBuddyContent  "$CONTENT_DB_PASSWORD"
create wb_quiz     WordBuddyQuiz     "$QUIZ_DB_PASSWORD"
create wb_progress WordBuddyProgress "$PROGRESS_DB_PASSWORD"
