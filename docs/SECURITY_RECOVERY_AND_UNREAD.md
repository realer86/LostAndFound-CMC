# Foundly — Quên mật khẩu và chấm đỏ đã đọc

> Phạm vi: sửa trên ZIP `LostAndFound-CMC(10).zip`. Không sửa `README.md` gốc, không đụng `.env`, không xóa dữ liệu PostgreSQL và không thay đổi thuật toán Matching.

## Thay đổi

- OTP đặt lại mật khẩu dùng bảng `PasswordResetTickets`, tách hoàn toàn khỏi `VerifyTokenHash`/OTP đăng ký.
- OTP được tạo bằng RNG mật mã, lưu hash, hết hạn sau 10 phút, khóa sau 5 lần sai, chỉ dùng một lần.
- Gửi lại: tối thiểu 60 giây/lần, tối đa 3 lần/giờ và 10 lần/24 giờ cho mỗi tài khoản; giới hạn thêm 10 request/5 phút/IP cho hai endpoint khôi phục; có giới hạn sai mã theo giờ.
- API yêu cầu khôi phục trả cùng thông báo cho email không tồn tại, chưa xác minh, bị khóa hoặc trong thời gian giới hạn. Backend không trả OTP qua HTTP.
- Mật khẩu đặt lại dài 12–128 ký tự, dùng `PasswordHasher<AppUser>` hiện có.
- Ghi audit `PASSWORD_RESET` (không chứa mã OTP hay mật khẩu). `SessionVersion` tăng sau khi đặt lại mật khẩu; cả cookie web và Admin đều được kiểm tra lại trong `OnValidatePrincipal`, làm cookie cũ không còn được chấp nhận.
- Khi `DEV_SEED_ENABLED=true`, chỉ 4 tài khoản `.invalid` được cập nhật mật khẩu nếu giá trị `DEV_SEED_PASSWORD` thay đổi; cũng thu hồi cookie test cũ.
- Phòng bàn giao, Hộp thư hỗ trợ (người dùng) và Hộp thư Admin cập nhật chấm đỏ sau khi API xác nhận tin nhắn đã đọc; giữ polling để nhận tin nhắn mới.
- Import `EyeOff` ở `admin/src/main.jsx` được viết đúng chữ hoa.

## Cập nhật database

Lệnh `docker compose up --build -d` sẽ chạy `backend/DatabaseScripts/08_password_recovery.sql` lúc khởi động API. Script **chỉ thêm** cột `Users.SessionVersion` và bảng `PasswordResetTickets`; không xóa volume hay dữ liệu cũ.

**Lưu ý:** Các cookie đăng nhập cũ được cấp trước khi có `SessionVersion` sẽ cần đăng nhập lại sau lần nâng cấp đầu tiên. Đây là thay đổi bảo mật có chủ đích.

## Kiểm tra trên máy Windows (trước khi merge)

```powershell
# Trên branch mới có chứa những sửa đổi này
python tests/dev_seed_contract.py
python tests/password_recovery_security_contract.py
python tests/unread_immediate_contract.py
python tests/school_auth_contract.py
python tests/official_support_contract.py

docker compose up --build -d
docker compose ps
docker compose logs api --tail=100
```

Kiểm thử chức năng bằng **tài khoản thật thử nghiệm** có email trường:

1. Yêu cầu Quên mật khẩu bằng email đã xác minh → nhận OTP với chủ đề **Mã đặt lại mật khẩu Foundly**.
2. Nhập OTP đúng, mật khẩu mới ít nhất 12 ký tự → đổi thành công; mật khẩu cũ không đăng nhập được.
3. Cookie đăng nhập cũ trên một trình duyệt/tab khác và cookie Admin của cùng tài khoản (nếu có) phải hết hiệu lực khi gọi API tiếp theo.
4. Nhập sai OTP 5 lần → mã bị khóa; nhập mã cũ lại không thành công. Kiểm tra mã hết hạn sau 10 phút và mã đã dùng không dùng lại được.
5. Yêu cầu gửi OTP mới (đợi ít nhất 60 giây): mã trước bị thay thế. OTP xác minh đăng ký của tài khoản khác không bị ảnh hưởng.
6. Nhập email trường chưa tồn tại → thông báo giống trường hợp email tồn tại; không có OTP trả về từ API.
7. Gửi quá 3 lần/giờ hoặc 10 lần/24 giờ cho cùng tài khoản → không gửi thêm email; yêu cầu hồi đáp chung để tránh lộ thông tin.
8. Nhận tin nhắn Phòng bàn giao, Hỗ trợ và Admin: mở hội thoại → chấm đỏ của hội thoại và trên thanh menu mất sau khi API xác nhận, không phải đổi tab. Với hội thoại khác còn chưa đọc, chấm đỏ tổng vẫn phải còn.
9. Đổi `DEV_SEED_PASSWORD` trong `.env`, khởi động lại API → 4 tài khoản test dùng mật khẩu mới, tài khoản thật không bị tác động. Đổi lại cùng giá trị không làm mất phiên đăng nhập test.

## Ranh giới kiểm thử và triển khai

- Các bài `*_contract.py` là **kiểm tra tĩnh**; không chứng minh SMTP, PostgreSQL, việc thu hồi cookie và luồng giao diện chạy thành công. Phải kiểm thử thủ công hoặc thêm integration tests chạy .NET + DB trước khi merge.
- Trên production cần HTTPS và `Cookie.SecurePolicy=Always`, CSRF protection phù hợp, chuyển `EnsureCreated` + script sang migration có quản lý, rate limiter **dùng chung giữa các replica** (Redis/API gateway), cấu hình reverse proxy tin cậy và bảo vệ SMTP secrets.
- Phản hồi yêu cầu khôi phục đã thống nhất về nội dung/mã HTTP, nhưng **không đảm bảo thời gian xử lý hoàn toàn đồng nhất** khi SMTP đang gửi. Nếu triển khai rộng nên đưa gửi mail sang hàng đợi có giám sát để tránh rò rỉ qua độ trễ.
- Không commit `.env`, file lưu mật khẩu SMTP, thư mục `.git`, `node_modules` hay dữ liệu database. Không dùng `docker compose down -v` để áp dụng thay đổi này.
