#!/usr/bin/env node
// .claude/hooks/format-cs.js
// PostToolUse hook: chạy `dotnet format whitespace` cho file .cs vừa Write/Edit.
// Không bao giờ chặn tool call — lỗi format chỉ bị bỏ qua.

const { execFileSync } = require('child_process');
const path = require('path');

let raw = '';
process.stdin.on('data', (chunk) => (raw += chunk));
process.stdin.on('end', () => {
  try {
    const filePath = (JSON.parse(raw).tool_input || {}).file_path || '';
    if (filePath.endsWith('.cs')) {
      const dir = path.dirname(filePath);
      execFileSync(
        'dotnet',
        ['format', 'whitespace', dir, '--folder', '--include', path.basename(filePath)],
        { cwd: dir, stdio: 'ignore', timeout: 50000 }
      );
    }
  } catch {
    // ignore: format is best-effort
  }
  process.exit(0);
});
