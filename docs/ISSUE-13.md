# Issue #13 — Liên hệ ẩn danh & Danh bạ (REQ-03, REQ-06) — Sprint 2

## Database

`Messages`: `Id (UUID, PK)`, `ReportId (FK Reports)`, `SenderId (FK Users)`, `RecipientId (FK Users)`, `Content (max 2000)`, `CreatedAt (UTC)`. Tất cả liên kết đều dùng FK, không tạo quan hệ User bằng Email/SĐT.

`OfficeDirectory`: `Id (UUID, PK)`, `Type (SECURITY/MANAGEMENT)`, `Name`, `Location`, `InternalPhone`, `WorkingSchedule`, `DutiesByTime` (công việc theo từng khung giờ). `InternalPhone` là **máy lẻ nội bộ**, không phải số cá nhân. 2 bản ghi có chữ `(demo)` chỉ được seed khi `ASPNETCORE_ENVIRONMENT=Development`.

SQL: `database/04_sprint23_upgrade.sql`, tự áp dụng khi khởi động `api` thông qua embedded resource. SQL này `CREATE TABLE IF NOT EXISTS`, **không DROP** các bảng Sprint 1. Chạy lại được.

## API (mọi endpoint yêu cầu cookie login)

| Method | Endpoint | JSON request | Result |
|---|---|---|---|
| POST | `/api/messages` | `{ "itemId": "UUID", "recipientId": "UUID", "content": "Xin chào..." }` | 201, tin nhắn đã lọc PII |
| GET | `/api/messages?itemId=UUID&otherUserId=UUID&page=1&pageSize=30` | — | 200, chỉ tin giữa **hai người** trên đúng bài |
| GET | `/api/contacts` | — | 200, `{ people: [...], offices: [...] }` |
| GET | `/api/contacts/directory` | — | 200, danh sách quầy/phòng nội bộ |

Các route **không có `/api`** (`/messages`, `/contacts`) cũng có để tương thích tên route của yêu cầu, nhưng frontend chuẩn của repo sử dụng `/api/...`.

### Phân quyền

- Người không đăng nhập → `401 Unauthorized`.
- Người dùng thường có thể gửi lời hỏi tới **chủ bài**; người sở hữu bài chỉ có thể trả lời người đã nhắn hoặc nộp Claim (không được gửi tràn lan tới người lạ).
- Sau khi một Claim được `APPROVED`, chỉ chủ bài và người được duyệt có thể gửi **tin mới** trên bài đó; các cuộc trò chuyện cũ vẫn truy xuất được bởi chính những người tham gia.
- Không đọc được hộp thoại giữa **hai người khác**, vì truy vấn bắt buộc cặp `SenderId/RecipientId` khớp caller và đúng `ReportId`.
- API chỉ xuất `UserId` và **bí danh**; không trả `FullName`, email hoặc SĐT cá nhân. Tin nhắn chứa email/số di động phổ biến sẽ bị từ chối `400`.
- Bí danh không phải danh tính thực; bộ lọc PII dạng regex chỉ là lớp bảo vệ cơ bản, **không thể phát hiện mọi cách nhập thông tin cá nhân dưới dạng văn bản**.

### Ví dụ Postman

Sau khi đăng nhập tài khoản Minh, lưu `user.id` làm `ownerId` trong Postman, rồi đăng nhập Lan.

```http
POST /api/messages
Content-Type: application/json

{"itemId":"<id bài OPEN>","recipientId":"<id Minh>","content":"Xin chào, mình muốn hỏi về món đồ."}
```

Mong đợi `201`. Sau đó dùng `GET /api/messages?itemId=<id>&otherUserId=<id Minh>` → `200` và không có email/SĐT thật. Với body chứa `0987654321`, API trả `400`.

### Những thứ không làm trong Issue này

Không có UI chat mới, không có WebSocket, SMS hoặc tích hợp tổng đài; issue chỉ yêu cầu database/API. Giao diện Sprint 1 cũ được giữ nguyên. Danh bạ demo cần thay bằng dữ liệu được trường xác nhận trước khi sử dụng thật.
