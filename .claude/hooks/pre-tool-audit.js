#!/usr/bin/env node
// .claude/hooks/pre-tool-audit.js
// PreToolUse/PostToolUse hook: ghi log mọi tool call, chặn thêm các lệnh
// Bash rõ ràng nguy hiểm (lớp phòng thủ bổ sung — không thay thế permissions.deny).

const fs = require('fs');
const path = require('path');

const LOG_DIR = path.join(process.env.CLAUDE_PROJECT_DIR || '.', '.claude', 'logs');
const LOG_FILE = path.join(LOG_DIR, 'audit.jsonl');

const DANGEROUS_PATTERNS = [
  /rm\s+-rf/i,
  /Remove-Item\s+.*-Recurse.*-Force/i,
  /git\s+push\s+--force/i,
  /DROP\s+(TABLE|DATABASE)/i,
  /docker\s+system\s+prune\s+-a/i,
  /terminate-instances/i,
];

let raw = '';
process.stdin.on('data', (chunk) => (raw += chunk));
process.stdin.on('end', () => {
  let input;
  try {
    input = JSON.parse(raw);
  } catch {
    process.exit(0); // input cannot be parsed  — no need to block, just skip logging
  }

  const toolName = input.tool_name || 'unknown';
  const toolInput = input.tool_input || {};
  const command = toolInput.command || '';

  fs.mkdirSync(LOG_DIR, { recursive: true });
  fs.appendFileSync(
    LOG_FILE,
    JSON.stringify({
      timestamp: new Date().toISOString(),
      tool: toolName,
      input: toolInput,
    }) + '\n'
  );

  if (toolName === 'Bash' && DANGEROUS_PATTERNS.some((p) => p.test(command))) {
    process.stderr.write('BLOCKED by pre-tool-audit hook: lệnh khớp pattern nguy hiểm.\n');
    process.exit(2);
  }

  process.exit(0);
});