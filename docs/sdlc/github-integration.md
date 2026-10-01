# Tích hợp GitHub Issues + GitHub Projects (Kanban) — chi phí 0đ

## Chi phí — cái gì free, cái gì không

| Thành phần | Giá | Ghi chú |
| --- | --- | --- |
| GitHub Issues, Labels, Milestones | Free | public và private repo |
| GitHub Projects v2 (board/table/roadmap) + built-in workflows | Free | auto-add, auto-move khi close/merge |
| Issue forms (`.github/ISSUE_TEMPLATE/*.yml`) | Free | |
| `gh` CLI | Free | Claude Code gọi qua Bash |
| GitHub Actions | Free cho public repo; private: 2.000 phút/tháng (plan Free) | chỉ dùng cho build/test, **không** gọi Claude |
| GitHub MCP server | Free | cần PAT; tùy chọn — `gh` đã đủ |
| `anthropics/claude-code-action` (Claude chạy trong Actions) | **Tốn phí** — tính theo API token | **Không dùng.** Thay bằng: Claude Code chạy local (subscription sẵn có) + `gh` |
| Jira / Trello | Free tier giới hạn automation/power-up | không cần — thêm 1 hệ thống thứ 2 |

➡ Kết luận: **Issues + Projects + `gh` CLI + Claude Code local** — không phát sinh chi phí.

## Bước 1 — Cài và đăng nhập `gh` (một lần)

Máy hiện **chưa có `gh`**.

```powershell
winget install --id GitHub.cli
gh auth login                      # GitHub.com → HTTPS → Login with a web browser
gh auth refresh -s project         # thêm scope đọc/ghi Projects v2
gh auth status
```

## Bước 2 — Labels (một lần)

```powershell
$r = "hunglu/WordBuddy"
gh label create "type:new"         -c 0E8A16 -d "New feature" -R $r
gh label create "type:change"      -c 1D76DB -d "Change to a shipped feature" -R $r
gh label create "type:bugfix"      -c D93F0B -d "Bug" -R $r
"identity","content","quiz","progress","notification","ui" | % { gh label create "svc:$_" -c C5DEF5 -R $r }
gh label create "audience:child"   -c FBCA04 -R $r
```

Status **không** dùng label — dùng field Status của Project (một nguồn sự thật duy nhất).

## Bước 3 — Issue forms

Đã có sẵn trong repo: `.github/ISSUE_TEMPLATE/idea.yml`, `bug.yml`. Sau khi merge vào `main` và
push, nút "New issue" sẽ hiện form. Các trường khớp 1-1 với `proposal.md` nên `/propose #n` chép
sang được.

## Bước 4 — Tạo Project (Kanban)

1. GitHub → avatar → **Your projects** → **New project** → template **Board** → tên `WordBuddy`.
2. Field **Status**: đổi options thành
   `Backlog · Ready · Planned · In progress · In review · Done · Released` (khớp `status:` trong proposal —
   bảng mapping ở `workflow.md`).
3. (Tùy chọn) thêm field `Service` (single select) và `Size` (S/M/L).
4. Project → **⋯ → Workflows** (built-in, free) — bật:
   - *Auto-add to project*: filter `is:issue` cho repo `hunglu/WordBuddy` → item mới vào **Backlog**.
   - *Item added to project* → Status **Backlog**.
   - *Item closed* → **Done**. *Pull request merged* → **Done**.
   - *Item reopened* → **In progress**.
5. Repo → tab **Projects** → **Link a project** → chọn `WordBuddy`.

Lấy số project cho CLI: `gh project list --owner hunglu`.

## Bước 5 — Vòng làm việc hằng ngày

| Việc | Ai / lệnh |
| --- | --- |
| Có ý tưởng | Tạo issue bằng form (web hoặc app điện thoại) → tự vào **Backlog** |
| Chọn việc | Kéo card sang **Ready** |
| Capture | Claude Code: `/propose #42` — Claude chạy `gh issue view 42`, điền `proposal.md` (`issue: 42`). Gõ `/propose <title>` không kèm số issue thì Claude tự tạo issue (`gh issue create --project WordBuddy`, cần Sam duyệt) và ghi số vào `issue:`; không có `gh` thì ghi `issue: pending` |
| Plan | `/plan <slug>` → `gh issue comment 42 -F .claude/plans/<slug>/plan.md` → kéo **Planned** |
| Code | `/code <slug>` → khi xong, Claude mở PR `gh pr create --base main --head feature/<slug>` (body có `Closes #42`, cần Sam duyệt) và ghi `pr:` vào proposal → **In progress** |
| Review | `/review <slug>` → reviewer đọc diff PR, ghi `review.md`, đăng lên PR bằng `gh pr review --comment` → kéo **In review**. `changes-requested` thì quay lại `/code` rồi `/review` |
| Test | `/test <slug>` (khi `reviewed`, hoặc `needs-fixes` nếu merge guard cho phép) → `gh pr merge <pr> --merge`; PR có `Closes #42` → issue tự đóng → **Done**. PR bị conflict thì giữ nguyên, không đổi status, không merge |
| Sửa sau review | `changes-requested` / `needs-fixes` → kéo card về **In progress**, chạy `/code <slug>`, rồi `/review` → kéo lại **In review** |
| Release | `/release` → kéo card sang **Released** |
| Xem tổng | `/status` (local) hoặc mở board |

Liên kết issue ↔ code theo quy ước: `issue:` trong frontmatter, `Refs #n` trong commit/PR,
`Closes #n` trong merge commit.

## Bước 6 (tùy chọn) — GitHub MCP thay cho `gh`

Chỉ cần nếu muốn Claude thao tác Projects qua tool thay vì gõ lệnh `gh`. Free, dùng
fine-grained PAT (quyền Issues / Pull requests / Projects read-write). Thêm vào `.mcp.json`:

```json
{
  "mcpServers": {
    "github": {
      "type": "http",
      "url": "https://api.githubcopilot.com/mcp/",
      "headers": { "Authorization": "Bearer ${GITHUB_PAT}" }
    }
  }
}
```

Đặt `GITHUB_PAT` bằng biến môi trường — không commit token. Bật bằng
`"enabledMcpjsonServers": ["github"]` trong `.claude/settings.json`.

## Không nên làm

- Không chạy Claude trong GitHub Actions (tốn phí API).
- Không quản lý status song song ở cả label lẫn Project field.
- Không sửa issue đã đóng để "đổi tính năng" — mở issue mới `type:change`, link issue cũ.
