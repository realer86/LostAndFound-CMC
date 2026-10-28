# Bản đồ nguồn gốc từ Foundly Pro (Final)

| Source Final | Source Sprint 1 | Giải thích |
|---|---|---|
| `api/Domain/Entities.cs` | `backend/Domain/Entities.cs` | Copy phần 6 entity Users, Categories, Locations, Reports, Images, CaseTickets, bỏ entity ngoài Sprint 1. |
| `api/Data/LostFoundDb.cs` | `backend/Data/LostFoundDb.cs` | Giữ mapping và quan hệ cho 6 bảng, loại những bảng ngoài scope. |
| `api/Controllers/AuthController.cs` | `backend/Controllers/AuthController.cs` | Giữ nguyên code đăng ký/xác minh/đăng nhập/logout. |
| `api/Controllers/ReportsController.cs` | `backend/Controllers/ItemsController.cs` | Trích DTO, validate, lấy bài, tạo bài; bỏ MatchEngine vì chưa làm matching. Đổi alias `/api/items`. |
| `api/Controllers/ReportsController.cs` | `backend/Controllers/CatalogController.cs` | Tách Catalog thành 2 API GET theo Issue #6. |
| `api/Controllers/UtilityController.cs` | `backend/Controllers/UploadsController.cs` | Copy UploadsController, bỏ NotificationsController. |
| `api/Program.cs` | `backend/Program.cs` | Giữ Cookie Auth, DbContext, EnsureCreated, seed; thu gọn và sửa seed độc lập. |
| `web/src/main.jsx` | `frontend/src/main.jsx` | Trích helper api, AuthModal, PostModal, ReportCard, Modal; viết lại App gọn để chạy độc lập. |
| `web/src/style.css` | `frontend/src/style.css` | CSS rút gọn theo mẫu màu giao diện, không chép toàn bộ CSS Final. |
| `api/Controllers/HandoversController.cs` | `ItemsController.MarkReturned` | KHÔNG copy quy trình bàn giao. Route trạng thái ở Issue #8 do yêu cầu mới, không thay thế xác minh Final. |
| Không có | `database/*.sql`, `docs/ERD.md` | Thêm script SQL, views, trigger, tài liệu 3NF cho Issue #1. |

**Những phần bị loại có chủ đích**: `MatchEngine`, `Claims`, `Handovers`, `Admin`, `Operations`, `CaseTickets`, `Notifications`, worker hết hạn, Admin frontend. Không thể ghép ngược module Sprint 1 vào Final bằng cách copy đè vì data/schema nhỏ hơn và nghiệp vụ RETURNED khác nhau.
