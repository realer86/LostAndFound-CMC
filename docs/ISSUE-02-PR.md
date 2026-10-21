# Mẫu mô tả Pull Request — Issue #2

## Tiêu đề đề xuất

`feat(sprint1): mockup responsive UI trang chủ và form đăng tin (#2)`

## Nội dung copy vào phần Description trên GitHub

### Mục tiêu

Hoàn thiện phần việc Sprint 1 — Issue #2: Wireframe, Mockup, HTML/CSS responsive cho Trang chủ và Form đăng tin.

### Đã hoàn thành

- Cắt UI tĩnh bằng HTML5 semantic, CSS responsive, media queries.
- Thiết kế Trang chủ gồm Hero, danh sách tin mẫu LOST/FOUND, tìm kiếm và bộ lọc.
- Thiết kế Form đăng tin: chọn loại tin, thông tin vật phẩm, khu vực, thời gian, mô tả và ảnh xem trước.
- Bộ kiểm thử trên 6 viewport từ 320px đến 1440px; không tràn ngang ở các màn hình được thử.
- Bổ sung wireframe/mockup desktop/mobile vào `docs/mockups/` và tài liệu `docs/ISSUE-02.md`.

### Cách xem thử

`http://localhost:5175/ui-homepage/index.html` (khi chạy Docker/Vite) hoặc mở `frontend/public/ui-homepage/index.html`.

### Phạm vi an toàn

- Chỉ thêm file, không sửa `README.md` gốc, `frontend/src/main.jsx`, API, DB, Docker hay source Sprint sau.
- Đây là preview **UI tĩnh**, không gửi dữ liệu lên server. Việc nối API là công việc tích hợp tiếp theo.

### Kiểm thử

- [x] Trang chủ + Form responsive trên 320, 375, 390, 768, 1024, 1440px (Playwright Chromium).
- [x] Bộ lọc / tìm kiếm hoạt động với dữ liệu mẫu.
- [x] Xem trước tin sau khi kiểm tra form hoạt động.
- [ ] Link Figma thật: **[dán tại đây sau khi share]**
- [ ] CI/merge conflict: **[kiểm tra lại trên GitHub sau khi mở PR]**

**Refs #2** (đổi thành `Closes #2` khi Figma được share, PR được nghiệm thu và sẵn sàng merge).
