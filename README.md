# Foundly Campus — Full Operations (localhost)

**Bộ project đầy đủ, không phải patch.** React (web sinh viên) + React Admin độc lập + ASP.NET Core Web API (.NET 10) + PostgreSQL, chạy qua Docker Compose. Đây là prototype đồ án phần mềm, **chưa phải dịch vụ công khai hoặc hệ thống quản lý tài sản có xác minh danh tính của trường**.

## 1. Khởi chạy trên Windows và GIỮ dữ liệu cũ

> Nên sao lưu database trước khi nâng cấp (xem mục 7). File Compose duy trì `name: foundly-complete`, cùng các volume `foundly-complete_lostfound_db` và `foundly-complete_lostfound_uploads` của Foundly Complete/Handover. Schema mới chỉ **bổ sung** bảng/cột, không chủ động xóa bài đăng, tài khoản, tin nhắn hay yêu cầu cũ. Dù vậy, nâng cấp schema luôn nên có backup.

1. Nếu bản cũ còn đang chạy: mở terminal tại thư mục chứa `compose.yaml` của bản cũ, chạy `docker compose down` (**không thêm `-v`**).
2. Giải nén ZIP vào thư mục tùy chọn, ví dụ `Downloads\Foundly_Final`. Mở đúng thư mục đó trong VS Code; trong Explorer phải thấy `api/`, `web/`, `admin/` và `compose.yaml` ở **cùng cấp**, không cần `cd` thêm nếu đã ở thư mục ấy.
3. Mở Docker Desktop (Engine running), rồi tại Terminal VS Code chạy:

   ```powershell
   docker compose up --build -d
   docker compose ps
   ```

4. Đợi container `postgres` healthy và `api`, `web`, `admin` đang Up. Mở trình duyệt:

| Thành phần | Đường dẫn |
|---|---|
| Sinh viên | http://localhost:5173 |
| Admin độc lập | http://localhost:5174 |
| API health | http://localhost:8080/api/health |

**Không** xóa Docker volumes, không dùng `docker compose down -v`, không chạy `docker system prune --volumes` nếu muốn giữ dữ liệu. Nếu gặp `Bind for 127.0.0.1:5433 failed`, hãy tắt đúng container Foundly cũ; hoặc đổi **chỉ** cổng host trong `compose.yaml` từ `127.0.0.1:5433:5432` sang `127.0.0.1:5434:5432` (backend vẫn dùng tên `postgres` cổng `5432`). Nếu lỗi HTTP 500, xem `docker compose logs --tail=100 api postgres`.

### Tài khoản thử nghiệm (chỉ Development)

| Vai trò | Email | Mật khẩu |
|---|---|---|
| LOST / người yêu cầu | `minh@campus.edu` | `Demo12345!` |
| FOUND / người giữ đồ | `lan@campus.edu` | `Demo12345!` |
| Admin | `admin@campus.edu` | `Admin12345!` |

Nên mở Chrome bình thường cho Minh, cửa sổ ẩn danh cho Lan. Admin dùng cổng 5174 với cookie độc lập. Không dùng mật khẩu demo khi triển khai trên mạng thật.

## 2. Nghiệp vụ đã có trong source code

| Nhóm | Chức năng |
|---|---|
| Tài khoản | Đăng ký/đăng nhập/đăng xuất, phân quyền User/Admin, tài khoản demo, giới hạn thao tác tài khoản bị tạm khóa |
| Bài đăng | LOST/FOUND, ảnh, chỉnh sửa, tìm kiếm, lọc, đóng bài, tự động hết hạn sau 30 ngày, gia hạn từ mục Bài của tôi |
| Ghép bài | Chỉ LOST↔FOUND, fuzzy weighted matching và candidate filtering, danh sách gợi ý và thông báo |
| Yêu cầu nhận đồ | Bằng chứng riêng tư, nhiều người có thể gửi claim, chỉ duyệt **một claim trên một bài FOUND** tại một thời điểm, rút claim/hủy chấp thuận nếu chưa bàn giao |
| Phòng bàn giao | Chat riêng, lịch đề xuất/chấp thuận/từ chối, đổi/hủy lịch, hai phía xác nhận vật lý |
| Ngoại lệ | Báo vắng mặt sau lịch hẹn, tranh chấp tạm khóa xác nhận; Admin bác báo cáo, mở lại phòng hoặc hủy bàn giao |
| Kiểm duyệt | Báo cáo bài vi phạm; Admin xem hồ sơ, ẩn bài, tạm khóa/mở khóa tài khoản, audit log |
| Bảo vệ trường | Người nhặt đề nghị ký gửi, Admin xác nhận **đã thực sự nhận** và cấp mã biên nhận, nhân viên ghi nhận **đã thực sự trao** khi có claim được duyệt và lịch gặp; người nhận vẫn phải xác nhận |
| Thông báo | Matching, claim, chat, lịch, nhắc lịch dưới 1h, khiếu nại, ký gửi, bài sắp/hết hạn; frontend làm mới chuông mỗi 12 giây |
| Admin | Dashboard, báo cáo 7 ngày, users/reports/claims/handovers, hàng đợi phản ánh, danh sách ký gửi, nhật ký hoạt động |

Các nút Admin “Đã trao tận tay” và các nút hai người “Đã giao/đã nhận” mô phỏng thủ tục ngoài đời thực; **chỉ nhấn sau khi việc trao nhận đã xảy ra trong thực tế**. Danh sách địa điểm bảo vệ hiện là ví dụ, phải được trường xác nhận trước khi vận hành.

## 3. Test nhanh 8 kịch bản

1. **Matching:** Minh đăng LOST "Ví da màu nâu", Lan đăng FOUND "Ví da" cùng vùng và giờ gần nhau → kiểm tra *Gợi ý phù hợp*; không được xuất hiện LOST↔LOST.
2. **Chuông:** kiểm tra thông báo cho cả hai tài khoản; có thể bấm Làm mới hoặc đợi 12 giây.
3. **Nhiều claim:** tạo thêm một tài khoản thường, hai người gửi yêu cầu cho cùng bài FOUND → Lan chỉ duyệt được một người. Người kia vẫn PENDING. Nếu chưa bàn giao, hủy duyệt để xử lý người khác.
4. **Phòng bàn giao:** Minh gửi bằng chứng, Lan duyệt, chat riêng, đề xuất lịch, Minh đồng ý. Hủy và đặt lại lịch; xác minh chỉ người còn lại được duyệt.
5. **Bàn giao thường:** sau buổi hẹn thực tế giả lập trong demo, Lan xác nhận đã giao, Minh xác nhận đã nhận → phòng COMPLETED và bài FOUND RETURNED. Một bên xác nhận trước không được hoàn tất.
6. **Tranh chấp:** ở phòng KHÁC chưa hoàn tất, báo tranh chấp hoặc NO_SHOW (chỉ sau giờ hẹn cho NO_SHOW) → phòng DISPUTED và tạm ngừng xác nhận; Admin → Kiểm duyệt & tranh chấp → xem và RESUME_ROOM hoặc CANCEL_HANDOVER có ghi lý do.
7. **Ký gửi:** Lan mở Hỗ trợ & ký gửi → chọn bài FOUND → chọn quầy → gửi yêu cầu. Admin → Quầy bảo vệ → xác nhận đã nhận và ghi mã. Khi claim đã duyệt + lịch đã chốt và nhân viên thật sự giao, Admin ghi RELEASE; Minh xác nhận tại Phòng bàn giao. Lan không được tự xác nhận thay nhân viên khi đồ còn ở quầy.
8. **Kiểm duyệt/hết hạn:** Báo cáo bài bất thường → Admin HIDE_REPORT hoặc DISMISS. Kiểm tra khóa/mở khóa User. Bài mở hơn 30 ngày sẽ được worker đánh dấu EXPIRED, chủ bài gia hạn trong Bài của tôi.

**Thời gian thử expiry:** worker chạy định kỳ 5 phút sau 35 giây khởi động đầu tiên; không cần chờ 30 ngày để hiểu thiết kế, nhưng thử thực tế cần đặt hạn trong database test. Không nên chỉnh database production tùy tiện.

## 4. Những giới hạn còn lại

- Backend chưa được biên dịch/chạy thử Docker E2E trong môi trường tạo source; **chạy `docker compose up --build -d` trên máy bạn là bước bắt buộc**. JSX của cả hai frontend đã qua kiểm tra cú pháp bằng TypeScript transpilation. Nếu build backend báo lỗi, gửi dòng log bắt đầu từ `error CS...` để chỉnh chính xác.
- Không có tích hợp email, SMS, SSO trường, WebSocket, payment hoặc danh tính sinh viên thật. Chat/chuông dùng polling 10/12 giây, không phải realtime tuyệt đối.
- Xác minh chủ sở hữu và biên nhận bảo vệ là quy trình demo; nhân viên cần kiểm chứng chứng cứ thật khi triển khai.
- Schema upgrade dùng SQL `IF NOT EXISTS` để tương thích cơ sở dữ liệu prototype cũ; hệ thống triển khai thật nên chuyển sang EF Core Migrations versioned, kiểm thử backup/restore, kiểm thử đồng thời và cấu hình security/HTTPS, secret manager, kiểm soát quyền nâng cao.
- Data cũ có thể đã ở trạng thái RETURNED do hành vi phiên bản trước; không tự thay đổi trạng thái hoàn tất đã có.

## 5. Cấu trúc

```text
compose.yaml
api/
  Controllers/{Reports,Claims,Handovers,Operations,AdminOperations,...}Controller.cs
  Data/LostFoundDb.cs
  Domain/Entities.cs
  Services/{MatchEngine,ReportExpiryWorker}.cs
web/src/{main.jsx,style.css}
admin/src/{main.jsx,style.css}
start-windows.bat
stop-windows.bat
README.md
```

## 6. Dừng ứng dụng (không mất dữ liệu)

```powershell
docker compose down
```

## 7. Sao lưu dữ liệu PostgreSQL trước nâng cấp

Trong PowerShell đặt ở **thư mục Compose cũ** (đang có container PostgreSQL), chạy:

```powershell
docker compose exec -T postgres pg_dump -U lostfound -d lostfound -Fc > foundly_before_upgrade.dump
```

PowerShell trên Windows có thể chuyển hướng nhị phân khác nhau tùy phiên bản; để bảo đảm, kiểm tra dung lượng và thử khôi phục bản dump trên database **thử nghiệm tách biệt** trước khi coi là backup hợp lệ. Không khôi phục đè database đang sử dụng khi chưa có kế hoạch sao lưu và thời gian dừng dịch vụ.

## 8. Debug nhanh

```powershell
docker compose ps -a
docker compose logs --tail=120 api
docker compose logs --tail=80 postgres
```

Nếu frontend hiện 401 sau khi bạn đăng xuất ở tab khác, hãy đăng nhập lại. Nếu muốn test hai User cùng lúc, dùng trình duyệt thường + ẩn danh để không dùng chung cookie. 
