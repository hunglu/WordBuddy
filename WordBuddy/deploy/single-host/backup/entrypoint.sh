#!/bin/sh
# Installs the 02:00 schedule and runs crond in the foreground.
set -eu
umask 077
{
  echo "export PGHOST='$PGHOST' PGUSER='$PGUSER' PGPASSWORD='$PGPASSWORD'"
  echo "export BACKUP_RETENTION_DAYS='${BACKUP_RETENTION_DAYS:-14}'"
} > /etc/backup.env
cp /backup/crontab /etc/crontabs/root
chmod 600 /etc/crontabs/root
exec crond -f -l 8
