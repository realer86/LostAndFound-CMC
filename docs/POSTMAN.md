# Kiểm thử bằng Postman — Sprint 1

Base URL: `http://localhost:8081` (host port của Compose Sprint 1).
Postman tự giữ cookie nếu sử dụng cùng host.

## 1. Kiểm tra danh mục

- `GET http://localhost:8081/api/categories` → kỳ vọng `200` và mảng 7 loại.
- `GET http://localhost:8081/api/locations` → kỳ vọng `200` và mảng 8 nơi.
- `GET http://localhost:8081/api/items/catalog` → kỳ vọng `{ "categories": [...], "locations": [...] }`.

## 2. Đăng nhập nhận cookie

`POST http://localhost:8081/api/auth/login`

Header: `Content-Type: application/json`

```json
{"email":"minh@campus.edu","password":"Demo12345!"}
```

Kỳ vọng: **200 OK**, Postman giữ cookie `foundly.sprint1.session`.

## 3. POST tạo tin

`POST http://localhost:8081/api/items`

Header: `Content-Type: application/json`

```json
{
  "type":"LOST",
  "itemName":"Ví sinh viên màu xanh",
  "description":"Thất lạc ở thư viện",
  "color":"xanh",
  "brand":null,
  "locationDetail":"Quầy tầng 1",
  "categoryId":null,
  "locationId":null,
  "eventTime":"2026-10-20T08:00:00Z",
  "privateIdentifier":null
}
```

**Sửa `eventTime` về thời điểm thực tế gần hiện tại, không lớn hơn hiện tại +2 giờ và không quá 5 năm trước.** Có thể thay `categoryId`/`locationId` bằng id có thật từ 2 API GET.

Kỳ vọng: **201 Created**, body chứa `id` (UUID) và `message`; Header `Location` trỏ tới `/api/items/{id}` hoặc `/api/reports/{id}`.

Nếu lỗi 401: đăng nhập và kiểm tra cookie trong Postman. Nếu lỗi 400: xem trường `message`, ngày/giờ, id danh mục/khu vực.

## 4. Upload ảnh tùy chọn

`POST http://localhost:8081/api/uploads/reports/{id}`

- Body → `form-data` → key `file` (loại File) → ảnh `.jpg`, `.png`, `.webp` hợp lệ.
- Mỗi file tối đa 5 MB, tối đa 4 ảnh/bài; kỳ vọng **200 OK** và URL ảnh.
- Chỉ người tạo bài đã đăng nhập mới upload được.

## 5. Các ca kiểm thử Issue #8 (chỉ stage cuối)

- `GET /api/items/mine` sau khi đăng nhập lấy `id` của bài do chính mình đăng.
- `PATCH /api/items/{id}/status` không cần body, khi bài đang `OPEN` → **200 OK**, `status=RETURNED`.
- `PATCH` lại → **409 Conflict**.
- Đăng nhập tài khoản khác và thử PATCH id đó → **403 Forbidden**.
- Không có cookie → **401 Unauthorized**.
- GET `/api/items` sau PATCH → bài có nhãn `RETURNED` để giao diện hiển thị màu xanh / mờ.

**Không đánh dấu tiêu chí kiểm thử là "đã chạy" cho đến khi có ảnh/log Postman thật.**

## Sprint 2/3 — Issue #13 và #17

Import `docs/Foundly_Issues_13_17.postman_collection.json`, chạy lần lượt các request; Postman phải lưu cookie login theo host `localhost:8081`. Xem thêm `docs/ISSUE-13.md` và `docs/ISSUE-17.md`.

Hoặc chạy `python tests/smoke_api.py` sau khi Docker đã khởi động (sử dụng 3 user demo Minh, Lan, Bảo). **Kết quả nghiệm thu không được giả định nếu chưa thực sự chạy.**
