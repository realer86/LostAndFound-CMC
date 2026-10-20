# Foundly Sprint 1 — Issues #1 + #6

Bản tách tối thiểu từ `Foundly Pro.zip`, lưu trong cấu trúc GitHub: `backend/`, `database/`, `docs/`, `frontend/`. Issue #8 sẽ bổ sung vào stage 3.

## Chạy Docker

Mở Docker Desktop, terminal tại gốc (nơi có `compose.yaml`):

```powershell
docker compose up --build -d
docker compose ps
docker compose logs --tail=80 api
```

- Frontend: **http://localhost:5175**
- API: **http://localhost:8081/api/health**
- Database: **localhost:5435** (`lostfound` / `lostfound` / `lostfound_dev_only`)
- Demo: `minh@campus.edu` / `Demo12345!`, `lan@campus.edu` / `Demo12345!`.

Frontend dùng cookie đăng nhập, Dropdown tự lấy `/api/items/catalog`, gửi `POST /api/items` và ảnh riêng. Các API chi tiết có trong `docs/POSTMAN.md`.

**Script SQL:** `01_schema.sql` tương đương bảng mà EF `EnsureCreated` tạo khi khởi động từ cơ sở dữ liệu trống. KHÔNG chạy script `01_schema.sql` trên DB chứa dữ liệu của Final. `03_views_triggers.sql` chạy qua DBeaver hoặc psql sau khi ứng dụng đã tạo bảng.

**Lưu ý:** Bản này cố ý không bao gồm thuật toán matching, claim, admin, phòng bàn giao, worker hết hạn từ Final. Code tạo bài trích từ Final nhưng bỏ lời gọi MatchEngine để module chạy độc lập. Trạng thái `RETURNED` do chủ bài bấm là mô phỏng trạng thái của Issue #8, không xác thực giao nhận thực tế.

Chỉ dùng localhost để nộp/bảo vệ. **Bản này chưa được chạy E2E bằng Docker/.NET trong môi trường đóng gói; cần chạy và chụp Postman trước khi đóng Issue.**
