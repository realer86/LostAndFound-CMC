# 🔍 Hệ thống Quản lý và Tìm kiếm Đồ thất lạc (Lost & Found System)

## 📖 Giới thiệu
**Hệ thống Quản lý và Tìm kiếm Đồ thất lạc** là một ứng dụng phần mềm được phát triển nhằm hỗ trợ sinh viên, giảng viên và cán bộ nhân viên trong khuôn viên Trường Đại học CMC dễ dàng đăng tải thông tin, tìm kiếm và nhận lại các vật dụng bị đánh rơi. 

Dự án được thực hiện trong khuôn khổ môn học **Công nghệ phần mềm**, áp dụng quy trình phát triển Agile/Scrum để quản lý vòng đời phần mềm.

## ✨ Tính năng nổi bật

### 👤 Dành cho Người dùng (Sinh viên/Giảng viên):
- **Báo mất đồ (Lost):** Đăng tải thông tin chi tiết (hình ảnh, mô tả, vị trí nghi ngờ rơi, thời gian) về vật dụng bị mất.
- **Nhặt được đồ (Found):** Đăng tin thông báo nhặt được đồ vật, hỗ trợ người mất có thể liên hệ để nhận lại.
- **Tìm kiếm thông minh:** Lọc tin đăng theo danh mục (Ví, thẻ sinh viên, thiết bị điện tử, chìa khóa...), thời gian hoặc khu vực tòa nhà.
- **Quản lý trạng thái:** Cập nhật trạng thái bài đăng cá nhân (Đang tìm kiếm, Đã tìm thấy, Đã trao trả).

### 🛡️ Dành cho Quản trị viên (Admin):
- **Kiểm duyệt nội dung:** Phê duyệt hoặc từ chối các bài đăng để đảm bảo thông tin chính xác, tránh nội dung rác.
- **Quản lý danh mục & người dùng:** Thêm/sửa/xóa các danh mục đồ vật, quản lý hoặc khóa các tài khoản có hành vi vi phạm.

## 🛠 Công nghệ sử dụng
*(Lưu ý: Xóa hoặc sửa các công nghệ dưới đây cho đúng với project của nhóm)*
- **Frontend:** HTML5, CSS3, JavaScript 
- **Backend:** Node.js / C++ / Java
- **Cơ sở dữ liệu:** SQL Server (Thiết kế chuẩn hóa 3NF)
- **Quản lý mã nguồn & Tiến độ:** Git, GitHub (Pull Request, Branching), Kanban Board.

## 🚀 Hướng dẫn cài đặt và chạy dự án

1. **Clone dự án về máy tính:**
   ```bash
   git clone https://github.com/realer86/LostAndFound-CMC.git
# Foundly Sprint 1 — Issues #1 + #6 + #8

**Source tách từ `Foundly Pro.zip` (Foundly Final Full Operations), không phải nguyên project Final.** Đây là một ứng dụng demo độc lập để nộp theo 3 Issue GitHub, có một Backend ASP.NET Core (.NET 10), React 19, PostgreSQL 17.

## Các Issue

- **#1:** thiết kế/ERD/3NF, 6 bảng `Users`, `Categories`, `Locations`, `Reports`, `Images`, `CaseTickets`, SQL view + trigger, khung Backend OOP. Xem `docs/ISSUE-01.md`.
- **#6:** `GET /api/categories`, `GET /api/locations`, `POST /api/items` trả `201 Created`, Form React + upload ảnh. Xem `docs/ISSUE-06.md`.
- **#8:** `PATCH /api/items/{id}/status`: chỉ tác giả và bài OPEN được chuyển sang RETURNED; nút trong modal chi tiết, nhãn xanh / làm mờ ngoài Trang chủ. Xem `docs/ISSUE-08.md`.

## Chạy bằng Docker

Mở Docker Desktop rồi terminal **tại thư mục chứa `compose.yaml`**:

```powershell
docker compose up --build -d
docker compose ps
docker compose logs --tail=100 api
```

| Dịch vụ | URL / kết nối |
|---|---|
| Frontend | http://localhost:5175 |
| Backend health | http://localhost:8081/api/health |
| API categories | http://localhost:8081/api/categories |
| API locations | http://localhost:8081/api/locations |
| Database | localhost:5435, db=`lostfound`, user=`lostfound`, pass=`lostfound_dev_only` |

Tài khoản demo do backend tạo **chỉ trong Development**: `minh@campus.edu` và `lan@campus.edu`, cùng mật khẩu `Demo12345!`. Đăng nhập trước khi đăng tin hoặc đánh dấu đã trả.

Compose đặt tên **`foundly-sprint1`**, dùng volume và host port riêng để không trùng database `foundly-complete` của bản Final. Không chạy `docker compose down -v` nếu muốn giữ dữ liệu sprint.

## Khởi tạo và SQL

Backend dùng `EnsureCreatedAsync()` để tạo 5 bảng từ EF Core khi dùng database trống. Script `database/01_schema.sql` là phiên bản SQL **độc lập** tương ứng; chạy trên DB test trống hoặc dùng kiểm tra thiết kế. Seed danh mục/khu vực từ Backend và `database/02_seed.sql` (có thể chạy nhiều lần). Các View + trigger trong `database/03_views_triggers.sql` phải được **kích hoạt riêng bằng SQL editor** nếu muốn dùng các đối tượng đó; không tự chạy theo Docker.

## Kiểm thử và nộp GitHub

- Các request và checklist Postman: `docs/POSTMAN.md`.
- Bản đồ code nào sao chép/tách/tùy chỉnh so với Final: `docs/SOURCE_MAP.md`.
- Cách giải nén snapshot thành 3 commit riêng trên **đúng** cấu trúc repo: `docs/GITHUB-NOP-BAI.md`.
- Chạy thử API/DB và UI bằng Docker + Postman trên máy bạn trước khi đánh dấu Issue "Done".

## Phạm vi / lưu ý quan trọng

- **Không có** MatchEngine, Claims, bàn giao 2 bên, Admin, expiry worker, moderation: ngoài phạm vi 3 Issue.
- API đánh dấu `RETURNED` của Issue #8 là **bổ sung cho Sprint 1**, không phải luồng "hai bên xác minh đã trao đồ" trong Final. Nó chỉ phản ánh chủ bài **đã tự xác nhận**.
- Dùng cho demo localhost. Không đưa mật khẩu demo, cookie HTTP hoặc `EnsureCreated` vào production.
- File được kiểm tra cấu trúc và mã nguồn tĩnh. **Chưa được chạy E2E bằng Docker/.NET trong môi trường tạo gói**; không coi checklist Postman là đã nghiệm thu.
