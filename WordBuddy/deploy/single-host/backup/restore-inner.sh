#!/bin/sh
# Runs inside the "restore" container. Usage: restore-inner.sh <YYYY-MM-DD>
set -eu
day="${1:?usage: restore <YYYY-MM-DD>}"
dir="/backups/$day"
[ -d "$dir" ] || { echo "no backup folder $dir" >&2; exit 1; }

restore_db() {
  db="$1"; role="$2"
  pg_restore --clean --if-exists --no-owner --role="$role" -d "$db" "$dir/$db.dump"
  echo "restored: $db"
}

restore_db WordBuddyIdentity wb_identity
restore_db WordBuddyContent  wb_content
restore_db WordBuddyQuiz     wb_quiz
restore_db WordBuddyProgress wb_progress

# Replace the media content with the backed up one.
find /media -mindepth 1 -delete
tar -xzf "$dir/media.tar.gz" -C /media
echo "restored: media"
