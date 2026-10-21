# Issue #6 – Database + GET Catalog + POST Form Đăng tin

- [x] Categories/Locations/Reports/Images/Users với FK ở `backend/Data/LostFoundDb.cs` (kế thừa Issue #1).
- [x] Dữ liệu gốc trong `backend/Program.cs` và `database/02_seed.sql`.
- [x] `GET /api/categories`, `GET /api/locations` (route mới cho acceptance criteria).
- [x] `GET /api/items/catalog` cung cấp hai Dropdown giống API gộp của bản Final.
- [x] `POST /api/items` (`POST /api/reports` là alias): validation và `Created(...)` trả HTTP **201 Created**.
- [x] Upload ảnh qua `POST /api/uploads/reports/{id}` sau khi tạo bài.
- [x] Form React trích từ `web/src/main.jsx` bản Final: tên, mô tả, chọn danh mục/vị trí, thời gian, màu, thương hiệu, ảnh.
- [ ] Xác thực 201 và response qua Postman thực tế (phải làm trên máy có Docker).

**Phân biệt nguồn:** API Create, model, validation và PostModal là những phần **trích/cắt giảm** từ Foundly Final. Tách `/categories`, `/locations`, định tuyến `/items`, thu gọn UI và loại phụ thuộc MatchEngine là **chỉnh sửa cần thiết**, không phải sao chép từng byte. Matching, Claims, Admin và Handovers không nằm trong Issue #6.
