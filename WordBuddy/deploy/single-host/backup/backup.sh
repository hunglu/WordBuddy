#!/bin/sh
# One backup run: pg_dump -Fc per database + the media volume, into /backups/<date>/ (files 0600).
# Then deletes backups older than BACKUP_RETENTION_DAYS (default 14).
# Manual run: docker compose exec backup sh -c '. /etc/backup.env && sh /backup/backup.sh'
set -eu
umask 077

day="$(date +%Y-%m-%d)"
dir="/backups/$day"
mkdir -p "$dir"

for db in WordBuddyIdentity WordBuddyContent WordBuddyQuiz WordBuddyProgress; do
  pg_dump -Fc --no-owner -d "$db" -f "$dir/$db.dump"
  echo "backup: $db done"
done
tar -czf "$dir/media.tar.gz" -C /media .
echo "backup: media done"
chmod 600 "$dir"/*

# find -delete removes the directory and its content, deepest first.
find /backups -mindepth 1 -maxdepth 1 -type d -mtime "+${BACKUP_RETENTION_DAYS:-14}" -exec find {} -delete \;
echo "backup: finished $day"
