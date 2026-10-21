# Nộp Issue #13 và Issue #17 lên GitHub

> Gói này ghép trực tiếp vào repo `realer86/LostAndFound-CMC` (nền Sprint 1). Không chép thư mục `backend` vào `backend/backend`.

1. Sao lưu project đang chạy và dữ liệu PostgreSQL (nếu có). Giải nén ZIP **bộ file thay đổi** trực tiếp vào thư mục gốc của repo, ở cùng cấp với `backend`, `database`, `docs`, `frontend`.
2. Tạo branch **từ nhánh có source Sprint 1 đầy đủ**. Trước khi nộp, bảo đảm `git status` sạch và `git pull` cập nhật. Nếu nhánh hiện tại đã có code Sprint 1 mới nhất, tạo branch từ nó (không bắt buộc chuyển về main nếu main chưa merge Sprint 1).
3. Chạy `docker compose up --build -d` tại thư mục gốc; kiểm tra `docker compose logs --tail=100 api` không có lỗi. Docker dùng DB volume Sprint 1 cũ và tự **thêm** bảng mới, không xóa dữ liệu.
4. Chạy `python tests/smoke_api.py` (Python 3.9+) hoặc import `docs/Foundly_Issues_13_17.postman_collection.json` vào Postman để kiểm tra API. Chỉ đánh dấu Issue đã được nghiệm thu **sau khi thử thành công**.

```powershell
git switch -c feature/sprint2-3-issues-13-17
git status
git add backend database docs tests README.md
git commit -m "feat: implement anonymous contacts and claim verification refs #13 #17"
git push -u origin feature/sprint2-3-issues-13-17
```

Mở Pull Request trên GitHub và mô tả: `Implements #13 and #17`. Khi đã review và kiểm thử xong, có thể dùng `Closes #13` / `Closes #17` trong PR nếu muốn tự đóng Issues khi merge. **Không tự đóng trước khi Postman/Docker chạy thực tế.**

## Phân biệt các gói

- `Foundly-Issues-13-17-CHANGED-FILES.zip`: chỉ **file mới hoặc sửa**, để giải nén đè lên code repo hiện tại (khuyên dùng).
- `Foundly-Issues-13-17-FULL-SOURCE.zip`: source app nền Sprint 1 + Issue #13/#17, có frontend Sprint 1, không chứa `.git`, không chứa database hoặc `.env` thật.

## Công bố tình trạng kiểm thử

Trong môi trường tạo ZIP không có .NET SDK/Docker, nên chỉ kiểm tra cấu trúc mã nguồn/tài liệu và cú pháp Python/JSON. File `tests/smoke_api.py` là kiểm thử **để chạy tại máy bạn**, không phải chứng cứ đã chạy E2E. Đừng đưa mật khẩu demo hoặc cookie không an toàn vào môi trường công khai.
