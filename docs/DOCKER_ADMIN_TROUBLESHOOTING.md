# Chạy Foundly Admin qua Docker trên máy nhóm

## Cấu hình đã kiểm tra

- `compose.yaml`: project mặc định `foundly-project`, Admin mở tại `127.0.0.1:5174`.
- `admin/Dockerfile` đã chạy `vite --host 0.0.0.0 --port 5174 --strictPort`.
- `admin/vite.config.js` cũng đặt `host: '0.0.0.0'`, `port: 5174`.
- Healthcheck của `admin` gọi HTTP nội bộ để phân biệt `Up` với `healthy`. Healthcheck **không tự giải quyết** xung đột host port.

## Máy mới clone repo

Trong PowerShell, chạy tại gốc dự án:

```powershell
docker compose config
docker compose up --build -d
docker compose ps
powershell -ExecutionPolicy Bypass -File .\scripts\check-admin.ps1
```

Đợi Admin lên trạng thái `healthy` và cột `PORTS` hiện `127.0.0.1:5174->5174/tcp`. Sau đó mở `http://localhost:5174`.

## Nếu báo `Bind for 127.0.0.1:5174 failed: port is already allocated`

Đây là lỗi **cổng trên máy chạy đang bị chiếm**, không phải lỗi cú pháp JSX hoặc Vite. Kiểm tra:

```powershell
docker compose ls -a
docker ps --format "table {{.Names}}\t{{.Ports}}"
netstat -ano | findstr :5174
```

Nếu còn project **cũ** `foundly-sprint1`, xác nhận không còn dùng trước khi dừng:

```powershell
docker compose -p foundly-sprint1 down
```

`down` không có `-v` sẽ không xóa named volumes. **Không** dùng `down -v` nếu muốn giữ dữ liệu test.

Nếu cần chạy song song một ứng dụng đang dùng cổng 5174, có thể đặt trong `.env` của **riêng máy đó**:

```dotenv
FOUNDLY_ADMIN_PORT=5176
```

Mặc định không có `.env` override thì Admin **vẫn ở 5174**. Khi override, địa chỉ truy cập dùng cổng mới. Đừng chỉnh cổng nội bộ `5174` trong Vite.

## Container `Up` nhưng trình duyệt vẫn báo không kết nối

```powershell
docker compose ps
docker compose port admin 5174
docker compose logs admin --tail=100
curl.exe -I http://localhost:5174
```

- Nếu cột `PORTS` chỉ là `5174/tcp` thì chưa có mapping ra host: kiểm tra file Compose đang dùng và khởi tạo lại bằng `docker compose up -d --force-recreate admin`.
- Nếu có mapping và Admin `unhealthy`, xem log Admin để biết Vite có khởi động lỗi không.
- Nếu mapping đúng và `healthy` nhưng host vẫn không vào được, kiểm tra firewall, VPN, proxy hoặc Docker Desktop trên máy đó. Sửa JSX/React không giải quyết lỗi TCP.

## Lưu ý khi đổi Compose project name

Đổi `name: foundly-sprint1` thành `name: foundly-project` làm Docker tạo **volume mới** như `foundly-project_foundly_sprint1_db`. Dữ liệu của volume cũ không bị xóa, nhưng **không tự được đưa vào database mới**. Không xóa volume cũ nếu còn cần dữ liệu.
