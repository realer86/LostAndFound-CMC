# Cách nộp 3 Issue theo 3 commit rõ ràng

## Cấu trúc đích

```text
<ten-repo>/
├── backend/
│   ├── Controllers/   (Auth, Catalog, Items, Uploads)
│   ├── Data/
│   ├── Domain/
│   ├── Program.cs
│   └── LostFound.Api.csproj
├── database/
│   ├── 01_schema.sql
│   ├── 02_seed.sql
│   └── 03_views_triggers.sql
├── docs/
│   ├── ERD.md
│   ├── ISSUE-01.md
│   ├── ISSUE-06.md
│   ├── ISSUE-08.md
│   ├── POSTMAN.md
│   └── SOURCE_MAP.md
├── frontend/
│   ├── src/main.jsx
│   └── src/style.css
├── compose.yaml
├── .gitignore
└── README.md
```

## Cách A: Commit đúng theo Issue (đề xuất)

**Nên có 3 ZIP snapshot:** Issue-01-foundation.zip → Issue-06-form.zip → Issue-08-returned.zip.
Mỗi snapshot là **nguyên cây gốc** của cùng repo, không phải 3 thư mục con. Bắt đầu từ `git clone` repo đúng.

1. Giải nén **nội dung bên trong** snapshot 01 **vào đúng root** repo (`backend/` sẵn có), cho phép ghi đè `README.md`/`.gitignore`, giữ `.git` nguyên vẹn.
2. Terminal từ root repo:

   ```powershell
   git add backend database docs frontend compose.yaml README.md .gitignore
   git commit -m "feat: backend database foundation refs #1"
   git push origin main
   ```

3. Giải nén snapshot 06 vào **cùng root repo** (ghi đè những file đã thay đổi), sau đó:

   ```powershell
   git add backend database docs frontend compose.yaml README.md .gitignore
   git commit -m "feat: posting form and item APIs refs #6"
   git push origin main
   ```

4. Giải nén snapshot 08 vào **cùng root repo**, sau đó:

   ```powershell
   git add backend database docs frontend compose.yaml README.md .gitignore
   git commit -m "feat: mark item returned refs #8"
   git push origin main
   ```

`refs #N` liên kết commit tới Issue mà không tự đóng; sau khi test thật, đổi status Issue thủ công hoặc tạo commit `closes #N` nếu có quyền/phù hợp quy trình nhóm.

## Cách B: Một commit duy nhất

Nếu nhóm chỉ cần nộp code cuối cùng, giải nén **Issue-08-returned.zip** vào gốc repo; chỉ tạo 1 commit:

```powershell
git status
git add backend database docs frontend compose.yaml README.md .gitignore
git commit -m "feat: deliver sprint 1 backend, posting, returned refs #1 refs #6 refs #8"
git push origin main
```

## Những điều tuyệt đối tránh

- ĐỪNG đưa ZIP vào `backend/` hoặc `frontend/`, đừng tạo lồng `backend/backend`.
- ĐỪNG xóa thư mục `.git` của repo đã clone, và đừng nộp `node_modules`, `bin`, `obj`, `dist`, `.env`.
- Nếu repo đang có code của các thành viên khác, hãy kiểm tra `git diff` và **KHÔNG ghi đè file đã được họ sửa**; có thể tạo branch và Pull Request để nhóm review.
- Dùng `git status`, `git diff --stat` trước khi `git add`. Nếu quyền push `main` bị khóa, dùng branch `sprint1/issues-1-6-8` và mở Pull Request.

## Test trước khi đóng Issue

```powershell
docker compose up --build -d
docker compose ps
docker compose logs --tail=100 api
```

- Mở http://localhost:5175.
- Kiểm tra API bằng Postman (`docs/POSTMAN.md`). Chụp response HTTP **201 Created** cho Issue #6, UI trước/sau khi đánh dấu `RETURNED` cho #8.
- Nếu cần xác thực SQL script, chạy nó bằng DBeaver trên DB **Sprint 1**, không chạy trên bản Final.
