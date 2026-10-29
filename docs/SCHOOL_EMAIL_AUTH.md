# Foundly – đăng ký/đăng nhập bằng email trường (Google + OTP)

Giữ nguyên `backend/`, `frontend/`, `database/`, Matching/Chat/Contacts và README gốc. Không xóa dữ liệu và không khởi tạo PostgreSQL volume mới.

## Chính sách tài khoản

- Sinh viên: `@st.cmcu.edu.vn` (STUDENT).
- Giảng viên: `@cmcu.edu.vn` (LECTURER).
- Khác (ví dụ `@gmail.com`, `@fakecmcu.edu.vn`, `@abc.st.cmcu.edu.vn`) bị **API** từ chối; không có tài khoản Guest.
- `accountType` là loại tài khoản được suy ra từ miền email, **không phải đặc quyền Admin**. `Role` hiện tại giữ là `USER` để không thay đổi kiểm soát truy cập của các Sprint trước.
- Google login chỉ chấp nhận ID token do Google xác minh, đúng `aud`, `iss`, `exp`, `email_verified` và domain. Người đăng nhập Google lần đầu được tạo trong bảng `Users` hiện tại. Nếu email đã có tài khoản mật khẩu thì có thể liên kết Google với email đã được Google xác minh. Không hỗ trợ tự tạo Admin.

## Cài đặt nhanh (localhost)

1. Sao lưu volume PostgreSQL trước khi nâng cấp; giữ nguyên `foundly_sprint1_db`.
2. Sao chép `.env.example` thành `.env`, điền `GOOGLE_CLIENT_ID` nếu dùng Google, điền SMTP nếu muốn **gửi OTP thật**. Không commit `.env`/mật khẩu ứng dụng.
3. Tạo Google OAuth Client loại **Web application**, cho phép JavaScript origin `http://localhost:5175` (nếu phát triển tại địa chỉ này); đặt cùng Client ID vào `.env`. Có thể cần cấu hình màn hình đồng ý OAuth và người dùng thử nghiệm theo chính sách Google Workspace của trường.
4. Chạy `docker compose up --build -d`, mở `http://localhost:5175`, tạo tài khoản bằng email trường. Google button chỉ hiện khi có Client ID.
5. `docker compose logs --tail=100 api` nếu gửi OTP lỗi HTTP 503. Kiểm tra kết nối SMTP, thông tin xác thực và email nhận (kể cả Spam).

Không có SMTP, API **không trả OTP vào response mặc định**. Chỉ khi thử nghiệm cô lập trên máy cá nhân, có thể đặt `AUTH_ALLOW_DEV_CODE=true` và **không** cấu hình SMTP: `devVerificationCode` được trả về. **Không triển khai cấu hình này ra internet.** Đặt lại `false` khi hoàn thành kiểm thử.

Các biến trong `.env` được Docker Compose chuyển vào backend: `Auth__Smtp__Host/Port/Username/Password/From`, `Auth__GoogleClientId`, `Auth__AllowDevCode`, cùng `VITE_GOOGLE_CLIENT_ID` ở frontend. Không đưa App Password vào `appsettings.json`. Nếu bí mật SMTP từng lộ trên GitHub, phải thu hồi và thay bằng mã mới.

## API

- `POST /api/auth/register` → email trường, tên, mật khẩu 10–128 ký tự; gửi OTP 6 chữ số, hạn 10 phút.
- `POST /api/auth/resend` → `{ "email": "..." }`; tối đa 1 OTP mới mỗi 60 giây cho mỗi tài khoản đang chờ xác minh.
- `POST /api/auth/verify` → email + code; tối đa 5 lần thử sai mỗi OTP.
- `POST /api/auth/login` → email và mật khẩu đã xác minh; chặn email ngoài trường và tài khoản bị khóa.
- `POST /api/auth/google` → `{ "credential": "Google ID Token" }`; token phải được backend xác minh trực tuyến với Google.
- `GET /api/auth/me`, `POST /api/auth/logout` → giữ nguyên contract cho frontend cũ.

Sau khi chạy backend, script bổ sung `backend/DatabaseScripts/05_school_auth.sql` chỉ thêm `OtpSentAt`, `OtpAttemptCount`, `GoogleSubject` vào **bảng Users đang có**. Không `DROP`, không xóa Users/Reports/Contacts/Messages. Trước khi triển khai production cần xây dựng EF Core migration và lộ trình backup/rollback riêng; `EnsureCreated` vẫn là cơ chế demo hiện tại.

### Hạn chế cần lưu ý trước khi production

- Cần HTTPS, CSRF protection phù hợp domain triển khai thực tế, và cấu hình cookie `Secure=Always` trên production. Origin guard hiện hữu chỉ dành cho localhost.
- Cần giới hạn toàn cục theo IP/người gửi (reverse proxy/API gateway) và theo dõi abuse SMTP; giới hạn resend/verify hiện tại chỉ theo tài khoản.
- Google `tokeninfo` là lời gọi HTTPS server-to-server, yêu cầu internet; production nên ưu tiên thư viện xác minh JWT bằng khóa công khai của Google để tránh phụ thuộc endpoint/giới hạn tốc độ.
- Thay đổi chính sách email không tự xóa tài khoản cũ. Cookie cũ sẽ bị backend từ chối; xử lý dữ liệu account cũ qua nghiệp vụ có kiểm soát sau khi sao lưu.
