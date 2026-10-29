# Foundly — kiểm thử nhiều tài khoản trên máy local

Bộ dữ liệu dưới đây **chỉ dành cho Development**. Không tạo tài khoản tại địa chỉ email thật của trường và không cần OTP/email để thử những người dùng mô phỏng này. Để kiểm thử OTP thực, tiếp tục dùng hộp thư trường được cấp cho bạn.

## Bật tài khoản kiểm thử

1. Từ thư mục gốc repository, tạo `.env` nếu chưa có: `Copy-Item .env.example .env` (nếu `.env` đã tồn tại thì **không ghi đè**).
2. Trong `.env`, đặt `DEV_SEED_ENABLED=true` và `DEV_SEED_PASSWORD=` với mật khẩu riêng dài **16–128 ký tự**. Mật khẩu được dùng cho cả bốn tài khoản **trong máy local**; không commit `.env` hoặc chia sẻ mật khẩu này.
3. Chạy `docker compose up -d --build api` và `docker compose logs --tail=60 api`.
4. Nếu vẫn gặp lỗi migration PostgreSQL thì phải sửa migration trước; bộ seed chỉ chạy sau khi khởi tạo bảng thành công.

| Email thử nghiệm | Vai trò | Đăng nhập ở |
| --- | --- | --- |
| `student01@foundly.invalid` | Sinh viên A | http://localhost:5175 |
| `student02@foundly.invalid` | Sinh viên B | http://localhost:5175 |
| `lecturer01@foundly.invalid` | Giảng viên | http://localhost:5175 |
| `admin@foundly.invalid` | ADMIN | http://localhost:5174 |

**Dùng đúng mật khẩu bạn đã đặt ở `.env`.** Script chỉ tạo các tài khoản chưa có, **không đổi mật khẩu tài khoản đã seed trước đó**. Nếu muốn đổi mật khẩu seed, phải có quy trình đặt lại riêng; thay đổi `.env` rồi chạy lại không đổi mật khẩu đã lưu.

Các địa chỉ `.invalid` là tên miền dành riêng cho kiểm thử, không nhận email và không thể đăng ký bằng endpoint thường. Trong Production, backend **không seed** và **không chấp nhận đăng nhập/cookie** cho các tài khoản thử này, ngay cả khi DB còn bản ghi. Các tài khoản CMC thật vẫn phải qua xác minh email.

## Kịch bản kiểm thử đề xuất

1. Chrome: đăng nhập `student01` và tạo bài LOST; Edge hoặc cửa sổ riêng: đăng nhập `student02` rồi tạo bài FOUND. Kiểm tra Matching.
2. `student01` gửi yêu cầu nhận đồ đối với bài FOUND của `student02`; `student02` duyệt. Kiểm tra Claims, Chat và phòng bàn giao.
3. Hai tài khoản xác nhận lịch hẹn/bàn giao; sau đó đăng nhập `admin` ở cổng 5174 để xem trạng thái và xử lý phản ánh, ký gửi.
4. Đăng nhập `lecturer01` để kiểm tra quyền người dùng giảng viên. Kiểm tra tài khoản ngoài miền trường bị từ chối khi đăng ký.
5. Đăng ký bằng **email trường thật** và kiểm tra OTP qua SMTP riêng; test account không chứng minh SMTP hoạt động.
6. Kiểm tra số bản ghi cũ trong `Claims` trước/sau để bảo đảm không bị mất dữ liệu.

## Tắt chế độ kiểm thử

Đặt `DEV_SEED_ENABLED=false` trong `.env`, sau đó chạy `docker compose up -d --force-recreate api`.

Bộ seed **không xóa** tài khoản hoặc dữ liệu đã tạo: khi tắt, backend không còn cho phép các tài khoản `.invalid` đăng nhập. Không sử dụng bộ `.env` local để triển khai website công khai. Nếu môi trường production dự định dùng cùng database, hãy làm sạch dữ liệu thử sau khi sao lưu, thay vì `docker compose down -v`.
