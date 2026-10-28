# Sprint 2 – Issue #15: Kết nối UI Chat & Danh bạ với API thật

## Đã sửa

- **Giữ hợp đồng #13**: `GET/POST /api/contacts`, `GET/POST /api/messages`, `GET /api/directory`.
- **Kế thừa UI #14**: `Sprint2ContactsView` nhận adapter `live`; khi không truyền `live`, màn hình demo tĩnh của #14 vẫn chạy độc lập.
- **Bỏ chat độc lập**: không còn render `<ChatLogic/>` ngoài `<App/>` và không gọi `http://localhost:5000/api/chats`.
- `ChatLogic.jsx` chỉ chứa hook `useChatLogic` và helper request. Frontend truy cập `/api/...` qua Vite proxy có sẵn, dùng cookie cùng origin.
- Khi chọn **Liên hệ người đăng**, tạo/tìm conversation bằng `POST /api/contacts` với `reportId` (server tự quyết định người nhận), rồi tải `GET /api/messages?contactId=...`.
- Sidebar hiển thị danh sách `items` của `GET /api/contacts`, chat dùng `senderId` để phân biệt tin gửi/nhận, gửi dùng `{contactId, content}`; phản hồi thành công được tải lại từ server.
- Danh bạ lấy `items` của `GET /api/directory`, không dùng số liên lạc cá nhân.
- Hỗ trợ làm mới thủ công, giới hạn bản xem 50 tin gần nhất; không giả vờ realtime hoặc lưu giả trên trình duyệt.
- Hiển thị lỗi từ API (401, 403, 404, 409, 429...) thay vì tạo tin nhắn local khi gửi thất bại.

## Các file trong bản sửa

- `frontend/src/components/ChatLogic.jsx` – Issue #15 adapter/hook (thay code API sai).
- `frontend/src/components/Sprint2ContactsView.jsx` – Thêm chế độ `live` vào UI #14, **vẫn giữ chế độ demo khi không có `live`**.
- `frontend/src/components/sprint2Contacts.css` – Một số style mới cho loading/error/refresh; giữ media query của #14.
- `frontend/src/main.jsx` – `ContactsScreen` dùng hook và view, bỏ root ChatLogic thứ hai.
- `tests/issue15_frontend_contract.py`, `tests/issue15_chat_api.test.cjs` – kiểm tra hợp đồng tích hợp và xử lý HTTP.

Không thay đổi backend Issue #13, database, file `README.md` gốc, Docker hoặc chức năng Sprint 3.

## Chạy kiểm tra offline

```powershell
python tests/issue13_schema_contract.py
python tests/issue14_ui_contract.py
python tests/issue15_frontend_contract.py
node --test tests/issue15_chat_api.test.cjs
```

## Chạy end-to-end trên máy có Docker

```powershell
docker compose up --build -d
docker compose ps
docker compose logs -f api
```

Truy cập `http://localhost:5175`, đăng nhập hai tài khoản demo ở môi trường Development (`minh@campus.edu` và `lan@campus.edu`, mật khẩu demo trong `backend/Program.cs`).

1. Tài khoản A đăng một bài mới.
2. Tài khoản B mở bài đó → **Liên hệ người đăng**. Màn hình chat phải chọn đúng bài và hiển thị hội thoại thật.
3. B gửi tin; chuyển sang tài khoản A → **Liên hệ & Danh bạ** → làm mới sidebar và chọn hội thoại. Tin của B hiển thị phía nhận.
4. A trả lời, B làm mới tin nhắn. Kiểm tra cả hai phía không lộ email hoặc thông tin xác minh bí mật.
5. Truy cập **Danh bạ** khi chưa đăng nhập: vẫn tra cứu được thông tin phòng ban từ API.
6. Đăng xuất, thử gửi tin trống, kiểm tra gửi quá nhanh (429), thử gửi sau khi bài đóng (409), và xem lỗi mạng để bảo đảm không sinh ra tin nhắn giả.

**Lưu ý:** API #13 không có WebSocket/realtime nên phải dùng nút làm mới. Kịch bản Docker, đăng nhập hai người và database phải được nghiệm thu trên môi trường chạy thật; các bài kiểm tra offline không thay thế bước này.
