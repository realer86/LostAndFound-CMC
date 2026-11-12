# Foundly: tin cậy và đánh giá công khai

## Giao diện
- Khối **Vì sao tin tưởng Foundly?** và **Đánh giá từ những lần trả đồ thật** được đặt ngay sau ô tìm kiếm, trước danh sách bài đăng; có liên kết ở hero.
- Trang **An toàn & Tin cậy** tách nút quay lại và kicker thành hai hàng, tránh đè chữ ở nhiều độ rộng màn hình.
- Khách chưa đăng nhập xem được `/api/reviews/public` và toàn bộ đánh giá ngay trên trang chủ. Khi chưa có dữ liệu, hiển thị thông báo trung thực, không tạo đánh giá mẫu hay số sao giả.
- Người dùng đã đăng nhập chỉ thấy biểu mẫu gửi đánh giá khi có ít nhất một phòng bàn giao đã hoàn tất mà họ tham gia và chưa đánh giá.

## API
- `GET /api/reviews/public` — công khai; trả `count`, `average` (null nếu chưa có), `items` tối đa 6 bản ghi mới nhất. Không trả `ReviewerId`, `HandoverRoomId`, email hoặc nội dung bằng chứng.
- `GET /api/reviews/eligible` — yêu cầu đăng nhập; trả những phòng bàn giao hợp lệ của tài khoản hiện tại chưa đánh giá.
- `POST /api/reviews` — yêu cầu đăng nhập. JSON: `{"handoverRoomId":"uuid","rating":5,"comment":"Trải nghiệm trả đồ thuận lợi"}`. Giới hạn 1–5 sao và nhận xét 10–500 ký tự. Chỉ hai phía của một lần bàn giao có `Status=COMPLETED`, `CompletedAt` và `HandedOverAt` mới được đánh giá. Mỗi người chỉ đánh giá một lần cho mỗi phòng.

## CSDL và triển khai
- Thêm bảng `FoundlyPublicReviews` và unique index `(HandoverRoomId, ReviewerId)`.
- `backend/DatabaseScripts/09_public_reviews.sql` được chạy tự động tại startup giống các script hiện tại. **Không xóa DB hay Docker volume**, không xóa đánh giá đã có; không thêm dữ liệu giả.
- Chạy từ thư mục gốc: `docker compose up -d --build`. Mở `http://localhost:5175` (hoặc cổng cấu hình trong `.env`).
- Để kiểm tra quyền khách: mở trình duyệt ẩn danh, vào trang chủ hoặc gọi `GET /api/reviews/public`; trả 200 ngay cả khi chưa đăng nhập. Thử gọi `POST /api/reviews` khi chưa đăng nhập phải trả 401.
- Để kiểm tra đánh giá có thật: hoàn thành bàn giao bằng cả hai tài khoản, đăng nhập một tài khoản, vào trang chủ và điền form. Sau đó mở tab ẩn danh để kiểm tra đánh giá mới xuất hiện.

Lưu ý: nhận xét là nội dung do thành viên cung cấp và đang được công khai ngay sau khi gửi; trước khi đưa lên môi trường production cần thêm quy trình kiểm duyệt và xử lý nội dung vi phạm. Không sử dụng EnsureCreated + SQL script như giải pháp migration production lâu dài.
