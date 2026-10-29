# Foundly – Hộp thư hỗ trợ theo hồ sơ và phân quyền phòng ban

## Thay đổi nghiệp vụ

- **Phòng bàn giao**: giữ nguyên trao đổi 1–1 sau Claim `APPROVED`, hẹn lịch, xác nhận giao/nhận.
- **Hỗ trợ & Danh bạ**: không cho mở hoặc gửi tin nhắn trực tiếp giữa hai người dùng. Endpoint cũ `POST /api/contacts` và `POST /api/messages` trả `410 Gone`; bản ghi cũ được bảo toàn và các endpoint đọc lịch sử có thể tiếp tục phục vụ đối soát.
- **Phản ánh trong Phòng bàn giao / báo cáo bài đăng**: tạo `CaseTicket` và một `SupportThread` đến `ADMIN`.
- **Yêu cầu ký gửi**: tạo `CustodyRecord` và `SupportThread` gắn với quầy đã chọn (`SECURITY`, `LIBRARY`, `DORM`).
- **Nhân viên**: Admin cấp vai trò `STAFF` và mã bộ phận cho một tài khoản đã xác minh. Nhân viên đăng nhập cổng `/admin` và chỉ thấy luồng đúng bộ phận. Admin nhìn thấy mọi luồng và có thể hỗ trợ điều phối. Không tự động cấp vai trò STAFF cho email trường.
- **Danh bạ**: tra cứu vị trí, giờ làm việc, số máy nội bộ đã cung cấp. Không tiết lộ liên hệ cá nhân.

## CSDL

Script `backend/DatabaseScripts/07_support_channels.sql` chạy khi API khởi động, thêm cột `Users.DepartmentCode`, bảng `SupportThreads`, `SupportMessages`. Các hồ sơ cũ trong `CaseTickets` và `CustodyRecords` được liên kết vào hộp thư hỗ trợ bằng cách backfill không nhân bản. Script không xóa bảng hoặc bản ghi cũ.

Sao lưu DB trước khi chạy lần đầu trên dữ liệu nhóm:

```powershell
# Từ thư mục dự án
# Không chia sẻ file dump ra ngoài

docker compose exec -T postgres pg_dump -U lostfound -d lostfound -Fc -f /tmp/foundly-support-backup.dump
docker compose cp postgres:/tmp/foundly-support-backup.dump .\foundly-support-backup.dump
```

## API

Tài khoản trường: `GET /api/support/threads`, `GET /api/support/threads/{id}/messages`, `POST /api/support/threads/{id}/messages`.

Cổng Admin/nhân viên: `GET /api/admin/support/threads`, `GET /api/admin/support/threads/{id}/messages`, `POST /api/admin/support/threads/{id}/messages`.

Chỉ Admin: `PATCH /api/admin/operations/users/{id}/department` với `{"department":"LIBRARY"}` / `SECURITY` / `DORM`; gửi `""` để thu hồi. Việc cấp quyền cần tài khoản đã xác minh và hiện chỉ áp dụng qua Admin. **Không tự đặt quyền trong giao diện sinh viên**.

## Kiểm thử thủ công sau khi chạy Docker

1. Dùng hai tài khoản test sinh viên tạo bài `FOUND`, gửi Claim, người giữ đồ duyệt; xác nhận vẫn nhắn tin được trong **Phòng bàn giao**.
2. Mở bài `LOST`/`FOUND`: không còn nút nhắn trực tiếp. Thử `POST /api/contacts` hoặc `POST /api/messages` qua một session hợp lệ: đều trả `410`, không sinh tin mới.
3. Trong Phòng bàn giao, báo tranh chấp: mục **Hỗ trợ & Danh bạ** xuất hiện thread `ADMIN`. Admin thấy thread trong `/admin` → Hộp thư hỗ trợ và trả lời. Người báo nhận được thông báo `SUPPORT` và đọc nội dung trả lời.
4. Dùng tài khoản khác đăng tin `FOUND` rồi chọn ký gửi **Thư viện**: hỗ trợ tạo thread `LIBRARY`. Admin trả lời được.
5. Admin cấp tài khoản đã xác minh vai trò `STAFF` / `LIBRARY`; đăng xuất, đăng nhập cổng Admin bằng tài khoản đó. Chỉ thấy luồng `LIBRARY`, không có dashboard hoặc API quản trị toàn bộ. Thử truy cập thread `SECURITY` trực tiếp phải nhận `404`.
6. Thử ký gửi ở **Bảo vệ**: chỉ tài khoản STAFF/SECURITY hoặc ADMIN mới được xem và trả lời.
7. Thu hồi quyền STAFF: phiên cũ phải bị vô hiệu sau khi xác thực lại cookie. Đảm bảo tài khoản sinh viên bình thường không đọc được luồng của người khác.
8. Kiểm tra số bản ghi `Claims`, `ReportClaims`, `Messages` trước/sau để đảm bảo không mất dữ liệu.

> Quan trọng: mới có kiểm thử cấu trúc và JSX trong môi trường đóng gói; .NET/Docker/PostgreSQL và bài test phân quyền thực tế phải được chạy trên máy có dịch vụ. Không merge vào `main` cho tới khi các bước ở trên đạt.
