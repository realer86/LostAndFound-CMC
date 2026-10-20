# Foundly Sprint 1 — Issue #1 (Backend & Database foundation)

Snapshot **giai đoạn 1**, chưa có Form/API đăng tin (Issue #6) và đánh dấu đã trả (Issue #8).

## Cấu trúc

- `backend/` ASP.NET Core Web API, Entity Framework Core + Auth.
- `database/` SQL tạo 6 bảng, seed và view/trigger PostgreSQL.
- `docs/` ERD, đối chiếu Issue #1.
- `frontend/` thư mục chờ Issue #6.

## Khởi chạy (Docker Desktop)

Mở Terminal ở thư mục chứa `compose.yaml`:

```powershell
docker compose up --build -d
docker compose ps
docker compose logs --tail=80 api
```

- Health API: http://localhost:8081/api/health
- PostgreSQL dùng localhost:5435, DB `lostfound`, user `lostfound`, password thử nghiệm `lostfound_dev_only`.
- Docker volume tên `foundly-sprint1_*` riêng, không ghi đè `foundly-complete` của bản Final.
- Khởi tạo bằng EF `EnsureCreated`, không cần chạy `01_schema.sql` để demo; script là tài liệu triển khai/kiểm tra.
- Muốn có view/triggers: kết nối bằng DBeaver/pgAdmin và chạy `database/03_views_triggers.sql` trên DB này (sau khi bảng đã có).

Đây là demo localhost. **Không dùng thông tin đăng nhập DB và seed demo trong môi trường production.** Chưa kiểm thử Docker runtime trong môi trường tạo gói này.
