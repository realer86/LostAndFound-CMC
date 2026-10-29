# Foundly – Claims, hỗ trợ ký gửi, phòng bàn giao và Admin Console

## Phạm vi

- Giữ nguyên `backend/`, `frontend/`, `database/`, Matching, Chat, Contacts và `README.md` gốc.
- Bật giao diện **Yêu cầu nhận đồ**, **Hỗ trợ & ký gửi**, **Phòng bàn giao**, **Thông báo** ở website sinh viên.
- Thêm Admin riêng tại `admin/`, cổng `http://localhost:5174`. Giao diện tham khảo bố cục Foundly Pro; API hoạt động với `backend/` hiện tại, không thay thế backend cũ.
- Database cập nhật bằng `backend/DatabaseScripts/06_operations.sql` (thêm bảng/cột nếu thiếu; không `DROP`, không xóa bản ghi).

## Chức năng Admin

1. Dashboard thống kê tài khoản, LOST/FOUND, bài đã trả, Claims, phản ánh, ký gửi; số cặp gợi ý là **mẫu tối đa 25 bài LOST × 25 bài FOUND**, không phải tổng số toàn hệ thống.
2. Quản lý bài đăng: xem, đóng/mở lại có kiểm tra Claims và hồ sơ ký gửi.
3. Người dùng: xem tình trạng xác minh, khóa/mở khóa có lý do.
4. Claims: xem trạng thái và bằng chứng riêng tư để hỗ trợ xác minh; việc duyệt vẫn do bên đang giữ đồ thực hiện.
5. Phòng bàn giao: theo dõi trạng thái hai bên, **không đọc nội dung tin nhắn riêng tư**.
6. Kiểm duyệt và tranh chấp: giải quyết phản ánh, ẩn bài hoặc xử lý phòng tranh chấp theo trạng thái.
7. Quầy ký gửi: nhận đồ thực tế, cấp mã biên nhận, xác nhận bàn giao tại quầy theo quy trình.
8. Audit: xem nhật ký xử lý admin.

## Bảo mật và cấp quyền

- Admin dùng **cookie `AdminCookie` riêng**, đường dẫn `/api/admin`; cookie đăng nhập website sinh viên không được dùng để gọi API quản trị.
- Tất cả API trong `api/admin/*` đều kiểm tra quyền `ADMIN` tại backend. Tài khoản `USER` và người chưa xác minh email không vào được.
- **Không có tài khoản Admin mặc định và không có mật khẩu demo.**
- Tài khoản đăng ký email trường và xác minh OTP, với **mật khẩu đã lưu** (không phải tài khoản chỉ dùng Google), mới có thể được người quản lý DB cấp quyền cho demo.
- Chỉ cấp quyền Admin cho tài khoản do nhóm kiểm soát. Không chia sẻ mật khẩu, OTP hoặc lưu mật khẩu trong repository.
- Admin console này dành cho demo phát triển trên localhost. Muốn triển khai thật cần cấu hình HTTPS, hostname/origin production, audit/monitoring và migration chính thức thay vì `EnsureCreatedAsync`.

## Chạy trên Windows PowerShell (Terminal VS Code)

1. Đảm bảo code đã được áp dụng vào nhánh riêng, có `.env` cấu hình SMTP để tạo và xác minh tài khoản nếu thử lần đầu.
2. Chạy:

```powershell
docker compose up --build -d
docker compose ps
```

3. Truy cập website sinh viên `http://localhost:5175` để đăng ký email trường và xác minh OTP; sau đó tạo tài khoản có mật khẩu (không cần Google).
4. Sau khi đã xác minh tài khoản **do nhóm quản lý** và có quyền thao tác DB, cấp vai trò Admin. Thay địa chỉ ví dụ dưới đây bằng **địa chỉ thực tế của tài khoản được phép**:

```powershell
docker compose exec postgres psql -U lostfound -d lostfound -c 'UPDATE "Users" SET "Role" = ''ADMIN'' WHERE "Email" = ''quantri@cmcu.edu.vn'' AND "EmailVerifiedAt" IS NOT NULL AND "PasswordHash" <> '''' RETURNING "Email", "Role";'
```

Nếu lệnh không trả về dòng nào, đừng bỏ điều kiện kiểm tra; hãy kiểm tra xem tài khoản đã đăng ký, xác minh OTP và có mật khẩu chưa. Lệnh này **chỉ dành cho quản lý DB trên máy phát triển**.

5. Mở `http://localhost:5174` và đăng nhập bằng email/mật khẩu Admin đã được cấp quyền.
6. Nếu gặp lỗi, xem log:

```powershell
docker compose logs --tail=100 api
```

## Kiểm thử thủ công cần chạy trước khi merge

- Đăng nhập website sinh viên bằng `USER`, thử `GET /api/admin/stats`: phải nhận `401`/`403` (không trả dữ liệu Admin).
- Đăng nhập Admin sai mật khẩu hoặc chưa xác minh OTP: phải từ chối.
- Đăng nhập Admin: thấy dashboard và các bảng. Đăng xuất: dashboard không còn truy cập được.
- Đăng tin LOST/FOUND, kiểm tra Matching, đóng/mở bài có ràng buộc và audit.
- Hai tài khoản sinh viên tạo/duyệt Claims; chat phòng bàn giao, đề xuất – xác nhận lịch; Admin chỉ xem trạng thái, không xem tin nhắn riêng.
- Tạo một phản ánh, Admin giải quyết và kiểm tra thông báo người báo.
- Đề xuất ký gửi; Admin chỉ xác nhận sau khi thật sự nhận vật phẩm và có mã biên nhận.
- Chạy kiểm thử hồi quy của frontend/Chat/Matching và `dotnet build backend/LostFound.Api.csproj`.

**Giới hạn kiểm thử môi trường tạo bản này:** Chỉ chạy kiểm tra tĩnh và phân tích cú pháp JSX. Chưa xác minh thực tế API/PostgreSQL/Docker, chưa build .NET hay Vite đầy đủ do thiếu phụ thuộc và SDK tại môi trường xây dựng. Trước merge cần kiểm thử trực tiếp trên máy phát triển của nhóm.
