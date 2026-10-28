# Matching MVP – triển khai độc lập từ `main`

**Phạm vi:** Chỉ Matching. Không thêm Google OAuth, REQ-04/Claims, bảng database hoặc hệ thống thông báo. Không sửa README gốc.

## Cách hoạt động

- Người dùng đăng nhập bằng tài khoản hiện tại và tạo bài `LOST` hoặc `FOUND`.
- Sau khi đăng, giao diện tự chuyển tới trang **Gợi ý phù hợp** và gọi API. Trang này cũng truy cập được từ thanh điều hướng hoặc mục **Bài của tôi**.
- API đọc các bài `OPEN` thuộc **loại đối lập**, bỏ bài của cùng tác giả; chỉ trả kết quả từ **50 điểm** và tối đa 10 bài (có thể đặt `limit` tối đa 20).
- Điểm `80–100`: *Rất phù hợp*, `50–79`: *Có thể liên quan*. Đây là **điểm tương đồng**, không phải xác suất đồ chắc chắn trùng hay bằng chứng sở hữu.
- Tính lại **khi người dùng vào trang / bấm Cập nhật / sau khi đăng tin**, không ghi kết quả xuống DB. Nếu ai đó đăng tin mới, mở/làm mới trang là nhận được kết quả mới; **chưa có thông báo đẩy**.
- Ghép dựa trên: tên/mô tả (35%), danh mục (20%), khu vực (20%), khoảng cách thời gian (15%), màu sắc (5%), thương hiệu (5%). Chuẩn hóa dấu tiếng Việt và một số từ đồng nghĩa: laptop/máy tính, xám/ghi, ví/bóp...
- Trường chưa nhập không bị chấm 0 trực tiếp mà tính theo những trường đã có, kèm hệ số độ đầy đủ dữ liệu; dữ liệu quá ít bị hạn chế xếp hạng cao.
- Chỉ chủ bài được gọi danh sách Matching cho bài của mình; thông tin riêng tư `PrivateIdentifier` **không xuất hiện** trong kết quả.

## Endpoint

```http
GET /api/reports/{id}/matches?minScore=50&limit=10
GET /api/items/{id}/matches?minScore=50&limit=10
```

Yêu cầu cookie đăng nhập. Phản hồi mẫu:

```json
{
  "reportId": "...",
  "threshold": 50,
  "matches": [
    {
      "id": "...",
      "score": 83,
      "level": "HIGH",
      "coverage": 0.85,
      "reasons": ["Cùng danh mục", "Màu sắc tương tự"],
      "report": { "id": "...", "type": "FOUND", "itemName": "Laptop màu ghi" }
    }
  ]
}
```

`minScore` từ 50 đến 100, `limit` 1–20. Kết quả `404` nếu bài không thuộc tài khoản đăng nhập, `401` nếu chưa đăng nhập. `report` thực tế chứa các trường công khai phục vụ trang chi tiết bài.

## Kiểm thử thủ công trên máy

```powershell
docker compose up --build -d
docker compose ps
python tests/matching_contract.py
```

1. Đăng nhập tài khoản demo thứ nhất (`minh@campus.edu`), đăng tin báo mất laptop Lenovo LOQ màu xám tại Thư viện.
2. Đăng nhập tài khoản demo thứ hai (`lan@campus.edu`), đăng tin nhặt máy tính Lenovo màu ghi tại Thư viện trong thời gian gần nhau.
3. Quay lại tài khoản thứ nhất, mở **Gợi ý phù hợp** và kiểm tra bài tương ứng xuất hiện (điểm phụ thuộc các trường đã nhập).
4. Thử bài không liên quan và kiểm tra không tạo gợi ý giả do chỉ trùng ngày/địa điểm.
5. Thử hai bài cùng `LOST`, hai bài cùng người đăng, bài đã `RETURNED`: không được Matching.
6. Mở tab Network: người chưa đăng nhập phải nhận `401`; người không sở hữu bài nhận `404`. Kiểm tra JSON không chứa `privateIdentifier`.
7. Kiểm tra các ô trống, chuỗi sai chính tả nhẹ, màn hình điện thoại và F5.

**Giới hạn bản MVP:** So sánh tối đa 1.000 bài đối lập mới nhất theo thời gian sự kiện mỗi lần gọi API. Khi quy mô tăng, cần cơ chế thu hẹp ứng viên dựa trên index hoặc tìm kiếm văn bản. Chưa có tập dữ liệu gắn nhãn để đo Precision/Recall và tối ưu các ngưỡng.
