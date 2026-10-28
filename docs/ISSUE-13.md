# Sprint 2 – Issue #13: API Liên hệ ẩn danh & Danh bạ

## Phạm vi

Bản triển khai **dựa trên LostAndFound-CMC Sprint 1** và kế thừa mô hình bảo vệ tin nhắn 2 phía của **Foundly Pro** (không chuyển quy trình bàn giao / matching / admin thuộc Sprint 3).

- `Contacts`: một cuộc trò chuyện cho mỗi cặp (bài đăng, người liên hệ); chỉ chủ bài và người liên hệ được đọc.
- `Messages`: ID liên hệ, ID bài đăng, ID người gửi/nhận, nội dung, thời gian tạo.
- `Directories`: tên đơn vị, vị trí, máy nội bộ, thời gian làm việc, nhóm, trạng thái.
- Endpoint yêu cầu cookie đăng nhập; **senderId / recipientId được server tự suy ra**, không nhận từ request.
- **Không đưa email, số điện thoại cá nhân, mật khẩu hay privateIdentifier vào DTO chat**.

## Database

- Với **database mới**: `EnsureCreatedAsync` tạo model đầy đủ theo EF Core.
- Với **database Sprint 1 có sẵn**: `Program.cs` tự chạy file `backend/DatabaseScripts/04_sprint2_contacts.sql` sau khi kết nối DB. SQL dùng `IF NOT EXISTS`, không DROP bảng.
- Script để chạy tay tương đương: `database/04_sprint2_contacts.sql`.
- Thông tin phòng ban (máy 1001/1002) **chỉ là dữ liệu demo** và chỉ được seed trong Development. Người dùng phải thay bằng dữ liệu trường đã xác minh trước khi vận hành thực tế.

## API contract cho Issue #15

| Method | Route | Nội dung |
| --- | --- | --- |
| POST | `/api/contacts` | Body `{ "reportId": "UUID" }`; chỉ người xem (không phải chủ bài) mở hội thoại khi bài OPEN |
| GET | `/api/contacts` | `{ "items": [{ "id", "reportId", "reportTitle", "otherUserId", "otherDisplayName", "createdAt", "lastMessageAt" }] }` |
| GET | `/api/contacts/{id}` | Chi tiết hội thoại cho đúng thành viên |
| POST | `/api/messages` | Body `{ "contactId": "UUID", "content": "Xin chào" }` |
| GET | `/api/messages?contactId=UUID&page=1&pageSize=50` | `{contactId, page, pageSize, total, items:[{id,contactId,reportId,senderId,recipientId,content,createdAt}]}` |
| GET | `/api/directory` | `{items:[{id,name,location,internalPhone,officeHours,category}]}`; công khai **máy nội bộ**, không có SĐT cá nhân |

Quy tắc: `POST /api/contacts` trả `201` khi tạo và `200` khi đã có; tự liên hệ trả `400`; bài không OPEN trả `404`; chat không thuộc người dùng trả `404`; chưa đăng nhập trả `401`; nội dung rỗng/quá 1500 ký tự trả `400`; gửi quá 15 tin/phút trả `429`; bài đã đóng trả `409` khi gửi, nhưng lịch sử vẫn đọc được.

## Chạy thử

```powershell
# Trong thư mục repo
# khởi động dịch vụ sau khi gộp Issue #13
docker compose up --build -d
python tests/issue13_contacts_api.py
```

Test sẽ tạo một bài thử nghiệm trên DB Development và dùng hai tài khoản demo. Không nên chạy trực tiếp trên DB production. Postman: import `docs/Foundly_Sprint2_Issue13.postman_collection.json`, chỉnh `baseUrl` và `reportId` nếu cần, đăng nhập bằng các tài khoản demo, chạy `Create contact` → `Send message` → `Message history` → `Directory`.

## An toàn và giới hạn

- Giữ cookie và Origin guard hiện hữu của Sprint 1; không bổ sung anonymous personal lookup.
- API chat chỉ xuất vai trò ẩn danh **“Người đăng tin” / “Người liên hệ”** và ID nội bộ để frontend phân biệt bubble; không xuất họ tên thật, email hay điện thoại.
- Không realtime/WebSocket, không push notification: thuộc giai đoạn sau.
- API list giới hạn 100 contacts, history giới hạn 50 tin một trang.
- Script idempotent phù hợp localhost demo, **không thay thế hệ thống migration được version hóa khi triển khai production**.
