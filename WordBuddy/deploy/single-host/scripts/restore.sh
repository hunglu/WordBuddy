#!/bin/sh
# Restores a backup made by the backup container. Usage: ./scripts/restore.sh <YYYY-MM-DD>
# Stops the APIs, restores the four databases and the media, then starts everything again.
# Run it from deploy/single-host/. This REPLACES the current data of the stack.
set -eu
day="${1:?usage: ./scripts/restore.sh <YYYY-MM-DD>}"
[ -d "./backups/$day" ] || { echo "no folder ./backups/$day" >&2; exit 1; }

printf 'This replaces ALL current data with the backup of %s. Type "restore" to continue: ' "$day"
read -r answer
[ "$answer" = "restore" ] || { echo "cancelled"; exit 1; }

docker compose stop wordbuddy-ui identity-api content-api quiz-api progress-api notification-api
docker compose up -d postgres
docker compose run --rm postgres-init
docker compose run --rm restore "$day"
docker compose up -d
echo "restore finished. Run ./scripts/health.sh"
