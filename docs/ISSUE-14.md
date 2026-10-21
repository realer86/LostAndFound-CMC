# Sprint 2 – Issue #14: UI Chat & Danh bạ

Giao diện được tích hợp vào React/Vite hiện tại trong `frontend/src`. Phong cách khung chat và bubble tham khảo phần handover chat của Foundly Pro, không đưa logic bàn giao của Sprint 3 vào đây.

## Phần đã dựng

- Menu **Liên hệ & Danh bạ**: switch tab Hộp thư / Danh bạ.
- **Liên hệ người đăng** trong chi tiết bài đang mở (chỉ hiển thị với người đã đăng nhập không phải tác giả) → khung chat trong popup.
- Danh sách hội thoại giả lập, phân biệt bubble **Bạn** / **Người đăng tin**; nhập và xem thử tin nhắn.
- Danh bạ hỗ trợ bằng thẻ thông tin, máy nội bộ và khung giờ mẫu; hỗ trợ tìm kiếm.
- Layout responsive 320px+; dùng semantic labels, tabs, focus-visible. CSS cô lập tên `.s2-*`.

## Dữ liệu / giới hạn

**Issue #14 là UI tĩnh**: mock messages và directory trong `Sprint2ContactsView.jsx`, tin nhắn chỉ giữ tạm trong state React. Không có POST API thật, không có lưu vào DB. Dữ liệu phòng ban có hậu tố `(demo)` và không phải số thực của CMC. **Issue #15** sẽ thay thế dữ liệu mẫu bằng `GET /api/contacts`, `GET /api/messages`, `POST /api/contacts`, `POST /api/messages` và `GET /api/directory` từ Issue #13.

## Chạy thử

```powershell
cd frontend
npm ci
npm run dev
```

Mở `http://localhost:5175`, chuyển tab **Liên hệ & Danh bạ**. Hoặc đăng nhập rồi mở chi tiết một bài do tài khoản khác đăng để xem nút **Liên hệ người đăng**.

## Checklist nghiệm thu

- [ ] Sidebar hội thoại + bubble gửi / nhận dễ phân biệt
- [ ] Nút “Liên hệ người đăng” xuất hiện đúng vị trí
- [ ] Danh bạ hiển thị thẻ phòng ban, vị trí, giờ, máy nội bộ
- [ ] UI không tràn ngang ở 320, 375, 768, 1280px
- [ ] Xem thử gửi tin không giả vờ gọi API thật
- [ ] `npm run build` thành công, test tương tác thủ công trên trình duyệt
