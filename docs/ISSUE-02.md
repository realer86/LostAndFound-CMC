# Issue #2 — Wireframe, Mockup và UI tĩnh Trang chủ / Đăng tin

> **Sprint 1 · Frontend · Giao diện độc lập**  
> Issue: https://github.com/realer86/LostAndFound-CMC/issues/2  
> Nhánh theo Issue: `feature/ui-homepage`

## 1. Mục tiêu và ranh giới

- Tạo Wireframe và Mockup cho **Trang chủ (tin LOST/FOUND)** và **Form đăng tin** trên desktop/mobile.
- Cắt giao diện bằng **HTML ngữ nghĩa + CSS**, dùng Media Queries và một lượng JavaScript nhỏ cho trải nghiệm bản mẫu.
- **Không** thay đổi `README.md` gốc, `frontend/src/main.jsx`, `frontend/src/style.css`, backend, database, Docker Compose hoặc code các Issue khác.
- Đây là **UI tĩnh xem thử**, chưa có đăng nhập, đăng bài thật, API gọi backend, tải ảnh lên server hoặc lưu vào cơ sở dữ liệu. Kết nối hệ thống thực được thực hiện ở issue tích hợp phù hợp (ví dụ Issue #6).

### Điểm quan trọng khi đánh giá

Tên ảnh chụp có thể bị hiểu thành “Form đăng nhập”, nhưng **mô tả và tiêu chí Issue #2 trên GitHub yêu cầu Form _đăng tin_**, nên nhánh này tập trung chính xác hai màn hình Trang chủ và Đăng tin.

## 2. Cấu trúc bàn giao

```text
frontend/
  public/
    ui-homepage/
      index.html      # HTML5 semantic: header, nav, main, section, article, form, aside, footer
      styles.css      # CSS scoped theo trang preview, responsive
      app.js          # Mock data, tìm kiếm, lọc, kiểm tra form, ảnh xem trước

docs/
  ISSUE-02.md         # Tài liệu này: phạm vi và nghiệm thu
  ISSUE-02-PR.md      # Mẫu mô tả Pull Request
  mockups/
    wireframe-trang-chu-desktop.svg
    wireframe-trang-chu-mobile.svg
    wireframe-dang-tin-desktop.svg
    wireframe-dang-tin-mobile.svg
    mockup-trang-chu-desktop.svg
    mockup-trang-chu-mobile.svg
    mockup-dang-tin-desktop.svg
    mockup-dang-tin-mobile.svg
    mockup-*.png      # PNG xuất từ SVG cho tham khảo
    ui-*.png          # Ảnh giao diện thật được render bằng Chromium

tests/
  issue02_ui_smoke.py # Test Chromium 6 viewport
```

## 3. Cách mở / demo

**Cách A — Không cần cài Node, Docker:** mở file `frontend/public/ui-homepage/index.html` bằng trình duyệt, hoặc dùng local HTTP server:

```powershell
cd frontend/public
python -m http.server 8000
```

Sau đó truy cập **http://localhost:8000/ui-homepage/index.html**.

**Cách B — Trong repo đang chạy bằng Docker:** mở Docker Desktop và chạy từ thư mục root có `compose.yaml`:

```powershell
docker compose up --build -d
```

Nếu web vẫn dùng Vite và cổng hiện tại của `main`, bản UI xem thử ở **http://localhost:5175/ui-homepage/index.html**. Ứng dụng React chính vẫn ở `/` và **không bị thay thế**.

**Cách dùng demo:** chọn `Tôi bị mất đồ` hoặc `Tôi nhặt được đồ` để vào Form; thử form bắt buộc, xem trước và chọn ảnh trong trình duyệt. Bấm “Trang chủ” để xem 6 tin minh họa; tìm bằng tên, lọc LOST/FOUND hoặc khu vực. Demo **không gửi dữ liệu lên server**.

## 4. Figma — bàn giao thiết kế

Đã tạo **8 SVG tham khảo**: 4 Wireframe (Trang chủ / Đăng tin × desktop / mobile) và 4 Mockup theo màu Foundly. Có kèm 4 ảnh PNG giao diện chạy trong trình duyệt để đối chiếu. Các vector SVG có thể được đưa vào Figma làm tài liệu thiết kế, nhưng **không phải link một file Figma đã được tạo và chia sẻ**.

Thao tác cho thành viên phụ trách Figma:

1. Mở Figma, tạo một file tên `Foundly · Sprint 1 · Issue #2 UI`.
2. Tạo 4 Frame: `Home / Desktop 1440`, `Home / Mobile 390`, `Post / Desktop 1440`, `Post / Mobile 390`.
3. Kéo lần lượt các file `mockup-*.svg` vào Frame tương ứng. Dùng file `wireframe-*.svg` để thể hiện quá trình thiết kế.
4. Tùy chỉnh/thay các vector, chữ thành component Figma có thể chỉnh sửa nếu cần. SVG nhập vào Figma không đồng nghĩa toàn bộ nội dung tự biến thành Figma Auto Layout.
5. Chọn **Share**, cấp quyền **Anyone with the link can view** hoặc quyền phù hợp với nhóm, sau đó **dán URL Figma thật** vào comment Issue #2 và PR. Không dán URL giả.

**Figma link: CHƯA CÓ — người có quyền Figma phải tạo và chia sẻ.**

## 5. Đặc điểm UI đã cắt

| Hạng mục | Thể hiện ở code |
|---|---|
| Semantic HTML | `<header>`, `<nav>`, `<main>`, `<section>`, `<article>`, `<form>`, `<fieldset>`, `<aside>`, `<footer>` |
| Trang chủ | Hero, CTA mất/nhặt, danh sách 6 tin mẫu, tìm kiếm, bộ lọc loại/khu vực |
| Form đăng tin | Loại tin, tên, danh mục, khu vực, ngày giờ, địa điểm, mô tả, ảnh tối đa 3 |
| Trạng thái UI | Kiểm tra form phía trình duyệt, xem trước bài đăng trong dialog; không có POST |
| Responsive | Media Queries tại `1080`, `850`, `600`, `365px`; menu thu gọn trên mobile |
| Riêng tư | Không tiết lộ dữ liệu xác minh đồ vật; ảnh chỉ xem trước trong trình duyệt |
| Sẵn sàng tích hợp | HTML/CSS/JS đặt **riêng** trong `frontend/public/ui-homepage` để tránh xung đột code React Sprint 1 |

## 6. Kết quả kiểm thử

Dùng Chromium Playwright (không yêu cầu Docker, backend). Các viewport được kiểm tra: **320×700, 375×812, 390×844, 768×1024, 1024×768, 1440×900**.

Các tiêu chí đã chạy và qua trên cả 6 viewport:

- Trang chủ render đủ 6 tin mẫu, không có thanh cuộn ngang.
- Chuyển lọc “Đồ nhặt được” còn 3 tin, tìm “tai nghe” còn 1 tin.
- Điều hướng vào Form đăng tin; Form không bị tràn ngang.
- Nhập dữ liệu hợp lệ, mở dialog xem trước, hiển thị đúng tên đồ vật, quay lại chỉnh sửa.
- Không có JavaScript pageerror trong những luồng nêu trên.

Chạy lại trên máy của nhóm khi đã có Playwright:

```powershell
pip install playwright
python -m playwright install chromium
python tests/issue02_ui_smoke.py
```

**Giới hạn kiểm thử:** Không có test kết nối backend / database vì cố ý ngoài phạm vi Issue #2. Các trình duyệt và thiết bị thật của nhóm vẫn cần kiểm tra thủ công trước khi merge.

## 7. Checklist nghiệm thu (trạng thái tại thời điểm tạo gói)

- [x] Wireframe desktop/mobile cho 2 màn hình.
- [x] Mockup SVG desktop/mobile cho 2 màn hình và ảnh giao diện thật.
- [x] HTML/CSS responsive và test tự động trên 6 viewport không bị tràn ngang.
- [x] Form đăng tin UI, input ảnh và xem trước (không đăng thật).
- [x] Code không sửa README gốc, API, DB hoặc phần UI React hiện hành.
- [ ] **Link Figma thật đã được chia sẻ cho nhóm** (chưa được tạo qua tài khoản Figma).
- [ ] **Đã push branch / mở PR lên repo thật** (cần chủ repo hoặc thành viên có quyền thực hiện).
- [ ] **PR không conflict** (chỉ xác nhận sau khi GitHub so sánh vào nhánh đích cập nhật).

**Không đánh dấu Issue #2 Done** cho tới khi 3 mục cuối đã hoàn thành và nhóm kiểm tra PR.
