# Issue #17 — Xác minh nhận đồ (REQ-04) — Sprint 3

## Database

`Claims`: `Id (UUID, PK)`, `ItemId (FK Reports)`, `ClaimerId (FK Users)`, `SecretText (tối đa 4000)`, `Status (PENDING / APPROVED / REJECTED)`, `CreatedAt`, `ReviewedAt`.

Ràng buộc và bảo vệ dữ liệu:

1. `UNIQUE(ItemId, ClaimerId)` ngăn cùng một người gửi lặp yêu cầu cho cùng bài.
2. `UX_Claims_OnlyOneApproved` là **unique partial index** trên `ItemId WHERE Status = 'APPROVED'`: ngay cả khi xử lý đồng thời, không thể có 2 Claim APPROVED trên một đồ.
3. Các thao tác `POST /claims` và `PATCH /claims/{id}/status` sử dụng transaction **có khóa hàng Reports theo ItemId (`FOR UPDATE`)** trước khi kiểm tra/ghi. Vì thế POST mới không thể chen vào lúc đang duyệt.
4. Khi duyệt một Claim, tất cả Claim `PENDING` khác cùng bài được cập nhật `REJECTED` **trong cùng transaction**, có `ReviewedAt`.
5. Chỉ nhận Claims cho bài `FOUND` đang `OPEN`; người đăng không được tự gửi yêu cầu.

## API

| Method | Endpoint | Request | Quyền và kết quả |
|---|---|---|---|
| POST | `/api/claims` | `{ "itemId": "UUID", "secretText": "Bằng chứng mô tả đồ ít nhất 12 ký tự" }` | người đăng nhập khác chủ bài; `201`, **không trả SecretText** |
| GET | `/api/items/{id}/claims` | — | **chỉ chủ bài** xem danh sách Claim và `secretText`; người khác `403` |
| PATCH | `/api/claims/{id}/status` | `{ "status": "APPROVED" }` hoặc `REJECTED` | **chỉ chủ bài**, khi Claim `PENDING`; `200` hoặc `409` |
| GET | `/api/claims/{id}` | — | chủ bài xem bằng chứng, chính người xin chỉ thấy trạng thái, người thứ ba `404` |
| GET | `/api/claims/mine` | — | trạng thái các yêu cầu của chính mình, **không có SecretText** |

Có alias các đường dẫn `/claims` và `/items/{id}/claims` tương ứng theo đúng cách ghi trong Issue. POST lấy `ClaimerId` từ cookie xác thực, **không tin** ClaimerId người dùng gửi ở body.

### Kịch bản nghiệm thu

- Minh đăng bài `FOUND`; Lan và Bảo mỗi người gửi `POST /api/claims` cho cùng `ItemId` → `201`, hai yêu cầu `PENDING`.
- Lan/Bảo `GET /api/items/{itemId}/claims` → `403`.
- Minh `GET /api/items/{itemId}/claims` → thấy đúng 2 yêu cầu, có bằng chứng riêng tư.
- Minh `PATCH /api/claims/{lanClaimId}/status` body `APPROVED` → `200`, trả `automaticallyRejected: 1`.
- Bảo `GET /api/claims/{baoClaimId}` → trạng thái `REJECTED`, **không có SecretText**.
- Thử duyệt Claim của Bảo thêm lần nữa → `409`; gửi claim sau khi có APPROVED → `409`.
- Kiểm tra SQL `database/05_sprint23_verify.sql`: truy vấn tìm 2 APPROVED trên một ItemId phải trả **0 hàng**.

### Lưu ý

Không tự đánh dấu bài RETURNED ngay sau khi duyệt: **duyệt quyền nhận không phải xác nhận bàn giao đã diễn ra**. Nút `MarkReturned` Issue #8 thuộc luồng demo Sprint 1. Chưa triển khai phòng bàn giao/trao đồ hai phía vì nằm ngoài các yêu cầu Issue #17.
