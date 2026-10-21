# Issue #8 – Nút “Đánh dấu đã trả đồ”

**Backend (phần của Đức)**

- [x] `PATCH /api/items/{id}/status` (có alias `/api/reports/{id}/status`) để chủ bài chuyển `OPEN` → `RETURNED`.
- [x] Yêu cầu đăng nhập (Cookie), chỉ người tạo bài được thao tác.
- [x] 404 nếu bài không tồn tại; 403 nếu tài khoản khác; 409 nếu đã trả/chưa ở trạng thái OPEN.
- [x] Dữ liệu giữ nguyên record, cập nhật `Status`/`UpdatedAt`, không xóa bài.

**Frontend (phần của Khiêm theo Issue)**

- [x] Nút **Đánh dấu đã trả đồ** xuất hiện trong **chi tiết bài đăng** chỉ khi đang đăng nhập đúng tác giả và bài ở trạng thái `OPEN`.
- [x] Nhấn nút gọi PATCH sau hộp thoại xác nhận.
- [x] Nhãn chuyển sang **ĐÃ TRẢ**, thẻ bài trên Trang chủ chuyển xanh/mờ. Bài ở trạng thái `RETURNED` tiếp tục hiển thị trong danh sách.
- [ ] Chạy thực tế, test tài khoản khác và chụp UI trước/sau.

**Khác biệt so với Final**: Trong Final, `RETURNED` chỉ sau khi hai bên hoàn thành quy trình bàn giao (ở `HandoversController`). Issue #8 muốn một nút do tác giả tự đánh dấu, nên **route `PATCH /items/{id}/status` là phần bổ sung của Sprint 1**, không phải hàm được sao chép từ Final. Demo chỉ xác nhận **tác giả đã đánh dấu**, không chứng minh giao nhận vật lý.
