#!/usr/bin/env node
// .claude/hooks/pre-tool-audit.js
// PreToolUse/PostToolUse hook: ghi log mọi tool call, chặn thêm các lệnh
// Bash/PowerShell rõ ràng nguy hiểm (lớp phòng thủ bổ sung — không thay thế permissions.deny).
// Log tách theo tháng: .claude/logs/audit-YYYY-MM.jsonl. Không ghi nội dung file (Write/Edit).

const fs = require('fs');
const path = require('path');

const LOG_DIR = path.join(process.env.CLAUDE_PROJECT_DIR || '.', '.claude', 'logs');

const DANGEROUS_PATTERNS = [
  /rm\s+-rf/i,
  /Remove-Item\s+.*-Recurse.*-Force/i,
  /git\s+push\s+--force/i,
  /DROP\s+(TABLE|DATABASE)/i,
  /docker\s+system\s+prune\s+-a/i,
  /terminate-instances/i,
];

const SHELL_TOOLS = ['Bash', 'PowerShell'];

// Only metadata is logged — file contents may hold secrets or child user data.
function summarize(toolName, toolInput) {
  if (SHELL_TOOLS.includes(toolName)) {
    return { command: toolInput.command };
  }
  if (toolInput.file_path) {
    return { file_path: toolInput.file_path };
  }
  return {};
}

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
  const now = new Date();
  const month = now.toISOString().slice(0, 7); // YYYY-MM (UTC)

  try {
    fs.mkdirSync(LOG_DIR, { recursive: true });
    fs.appendFileSync(
      path.join(LOG_DIR, `audit-${month}.jsonl`),
      JSON.stringify({
        timestamp: now.toISOString(),
        event: input.hook_event_name,
        tool: toolName,
        input: summarize(toolName, toolInput),
      }) + '\n'
    );
  } catch {
    // logging must never block a tool call
  }

  if (SHELL_TOOLS.includes(toolName) && DANGEROUS_PATTERNS.some((p) => p.test(command))) {
    process.stderr.write('BLOCKED by pre-tool-audit hook: lệnh khớp pattern nguy hiểm.\n');
    process.exit(2);
  }

  process.exit(0);
});
