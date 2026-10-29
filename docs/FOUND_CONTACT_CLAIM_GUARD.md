# FOUND: xác minh trước, trao đổi sau

- Với bài **FOUND**: người nghi là chủ sở hữu phải gửi bằng chứng riêng qua **Yêu cầu nhận đồ**.
  Khi người nhặt **duyệt** Claim, hệ thống mở **Phòng bàn giao** cho đúng hai người.
  Chưa gửi, đang chờ, bị từ chối, hoặc đã hủy Claim không được mở hộp thư trực tiếp.
- Với bài **LOST**: người có thông tin có thể liên hệ người đăng bằng **Hộp thư**.
- `POST /api/contacts` trả HTTP 409 đối với **FOUND** (kể cả đã có Claim);
  `GET /api/contacts`, `GET /api/contacts/{id}`, `GET/POST /api/messages`
  không cho sử dụng cuộc trò chuyện trực tiếp thuộc FOUND cũ. Dữ liệu lịch sử **không bị xóa**.
- Hệ thống không phải thay đổi schema PostgreSQL, sửa `README.md` hay đổi Matching/Auth.

## Kiểm tra

```bash
python tests/found_contact_gate_contract.py
python tests/system_smoke_e2e.py   # Cần API và dữ liệu Development phù hợp
```

Test thủ công: đăng nhập hai tài khoản, tạo bài FOUND, xác nhận không có nút hộp thư;
`POST /api/contacts` với ID bài FOUND phải nhận HTTP 409; tạo Claim PENDING không mở phòng;
duyệt Claim rồi mở Phòng bàn giao. Tạo bài LOST và xác nhận hộp thư vẫn hoạt động.
