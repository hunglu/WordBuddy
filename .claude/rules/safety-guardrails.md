# Safety Rules — WordBuddy backend

- Never suggest or run Claude Code with the `--dangerously-skip-permissions` flag on this machine.
- Any action listed under `ask` in `.claude/settings.json` (DB migrations, docker compose, git push)
  must stop and wait for Sam's confirmation—do not automatically retry or attempt to bypass it with an equivalent command.
- Do not read or print the contents of `appsettings.Production.json`, `.env`, connection strings, or JWT signing keys to output.
- `CLAUDE.md` files are ask-gated for Edit/Write. Never change them through Bash (`sed`, `echo >>`,
  `Set-Content`) to get around that, and never append run output, progress, or history to them —
  propose the exact change to Sam instead.
- When an action is blocked by `deny` or `ask`, briefly explain why and suggest a safer alternative;
  do not repeat the same command in another form to evade permission checks.