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

Board không cần kéo tay nữa: mỗi lệnh tự chạy **Board sync** (xem phần dưới) sau khi đổi `status:`.

| Việc | Ai / lệnh |
| --- | --- |
| Có ý tưởng | Tạo issue bằng form (web hoặc app điện thoại) → tự vào **Backlog** |
| Capture | Claude Code: `/propose #42` — Claude chạy `gh issue view 42`, điền `proposal.md` (`issue: 42`). Gõ `/propose <title>` không kèm số issue thì Claude tự tạo issue (`gh issue create --project WordBuddy`, cần Sam duyệt) và ghi số vào `issue:`; không có `gh` thì ghi `issue: pending`. Board sync → **Ready** |
| Plan | `/plan <slug>` → `gh issue comment 42 -F .claude/plans/<slug>/plan.md` → board sync **Planned** |
| Code | `/code <slug>` → lúc bắt đầu: assign issue cho `@me` + board sync **In progress**. Khi xong: một commit + một push, mở PR `gh pr create --base main --head feature/<slug>` (body có `Closes #42`, cần Sam duyệt), ghi `pr:` vào proposal trong working tree (commit ở stage sau) |
| Review | `/review <slug>` → reviewer đọc diff PR, ghi `review.md`, đăng lên PR bằng `gh pr review --comment` → board sync **In review** (approve) hoặc **In progress** (changes requested) |
| Test | `/test <slug>` (khi `reviewed`, hoặc `needs-fixes` nếu merge guard cho phép) → `gh pr merge <pr> --merge`; PR có `Closes #42` → issue tự đóng → **Done** (board sync chỉ kiểm tra). `needs-fixes` → board sync **In progress**. PR bị conflict thì giữ nguyên, không đổi status, không merge |
| Sửa sau review | `changes-requested` / `needs-fixes` → `/code <slug>` (fix round, board sync **In progress**), rồi `/review` |
| Release | `/release` → board sync **Released** |
| Xem tổng | `/status` (local) hoặc mở board |

Liên kết issue ↔ code theo quy ước: `issue:` trong frontmatter, `Refs #n` trong commit/PR,
`Closes #n` trong merge commit.

## Board sync

Recipe dùng chung cho mọi lệnh workflow (`/propose`, `/plan`, `/code`, `/review`, `/test`,
`/release`). Các lệnh chỉ link tới đây, không chép lại.

**Prerequisite:** `gh` đã cài và token có scope `project` (`gh auth refresh -s project`).

### Mapping `status:` ↔ cột Status

| `status:` in proposal.md | Board Status | Set by |
| --- | --- | --- |
| `idea` | Ready | `/propose` |
| `planned` | Planned | `/plan` |
| `in-progress`, `changes-requested`, `needs-fixes` | In progress | `/code` start, `/review` (changes requested), `/test` (needs fixes) |
| `implemented` | In progress | (no change) |
| `reviewed` | In review | `/review` (approve) |
| `done` | Done | `/test` after the merge — usually already set by the built-in "PR merged / item closed → Done" workflow, so this is a check |
| `released` | Released | `/release` |
| `blocked` | unchanged | — |

### Recipe

```bash
# read-only, not gated: resolve ids once per run
gh project list --owner hunglu --format json                         # → project number + id for "WordBuddy"
gh project field-list <num> --owner hunglu --format json             # → Status field id + option ids by name
gh issue view <issue> -R hunglu/WordBuddy --json projectItems        # → is the issue on the board?
gh project item-list <num> --owner hunglu --format json --limit 200  # → item id for the issue (match content.number)
# gated writes (ask in .claude/settings.json)
gh project item-add <num> --owner hunglu --url <issue url>           # only if not on the board yet
gh project item-edit --project-id <pid> --id <item id> --field-id <status field id> --single-select-option-id <option id>
gh issue edit <issue> -R hunglu/WordBuddy --add-assignee @me         # /code start only (skip if already assigned to @me)
```

### Rules

- Skip board sync when `issue:` is `none`/`pending`.
- If `gh` is missing, lacks the `project` scope, or Sam declines a write: say so and continue.
  Board sync never blocks a stage and never changes `status:`.
- Look option ids up by **name**. If the mapped option name doesn't exist on the board, report
  it and skip — never pick a "closest" option.
- Board sync is a GitHub write, not a git commit — it adds no commits.
- Assignee is `@me` (the gh-authenticated account), never a hard-coded login.

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
