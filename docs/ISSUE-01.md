# Issue #1 – Thiết kế cơ sở dữ liệu và cấu trúc Backend

- [x] Có EF Core Entities, DbContext, khóa chính, khóa ngoại (`backend/Domain`, `backend/Data`).
- [x] Sơ đồ ERD mô tả quan hệ, thảo luận 3NF (`docs/ERD.md`).
- [x] Script SQL PostgreSQL (`database/01_schema.sql`, `02_seed.sql`, `03_views_triggers.sql`).
- [x] Khởi tạo khung OOP theo Controllers / Data / Domain, chạy .NET 10.
- [x] `.gitignore` và Docker Compose tách khỏi bản Final (volume riêng).
- [ ] Thực thi script / build bằng Docker trên máy nộp, kiểm tra log và chụp minh chứng.

**Nguồn code:** trích `api/Domain/Entities.cs`, `api/Data/LostFoundDb.cs`, `api/Controllers/AuthController.cs`, `api/Program.cs` từ Foundly Final. Các bảng và cấu hình được rút gọn về Sprint 1; script, ERD, README, trigger là phần bổ sung cho Issue #1.

**Chạy riêng stage này:** `docker compose up --build -d`, truy cập `http://localhost:8081/api/health`. `EnsureCreated` sẽ tạo 6 bảng và chương trình seed danh mục/khu vực; view/trigger có thể kích hoạt qua SQL script số 03 trên DB đã khởi tạo.
