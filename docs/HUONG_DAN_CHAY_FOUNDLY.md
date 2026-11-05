# Hướng dẫn chạy Foundly sau khi clone GitHub

**Áp dụng:** Windows PowerShell. Máy đã cài Git, Docker Desktop và đang mở Docker Desktop.

## 1. Clone source code

Mở PowerShell tại thư mục muốn lưu dự án:

```powershell
git clone https://github.com/realer86/LostAndFound-CMC
cd LostAndFound-CMC
```

## 2. Tạo và cấu hình `.env`

```powershell
Copy-Item .env.example .env
notepad .env
```

Để chạy Foundly và đăng ký bằng OTP gửi tới email thật, điền thông tin tài khoản SMTP của bạn vào các biến tương ứng trong `.env`. Ví dụ với Gmail:

```dotenv
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=youraccount@gmail.com
SMTP_PASSWORD=your_16_character_app_password
SMTP_FROM=youraccount@gmail.com
AUTH_ALLOW_DEV_CODE=false
DEV_SEED_ENABLED=false
DEV_SEED_PASSWORD=
```

Thay các giá trị ví dụ bằng thông tin thật. Nếu chưa cấu hình SMTP, các container vẫn có thể khởi động nhưng chức năng gửi OTP thật sẽ không hoạt động. **Không commit hoặc chia sẻ file `.env` lên GitHub.**

## 3. Build và chạy Docker

Đứng tại thư mục `LostAndFound-CMC`, chạy lần lượt:

```powershell
docker compose -f compose.yaml config --quiet
docker compose -f compose.yaml build
docker compose -f compose.yaml up -d
docker compose -f compose.yaml ps
```

## 4. Mở Foundly

- Trang người dùng: http://localhost:5175
- Trang quản trị: http://localhost:5174
- Kiểm tra API: http://localhost:8081/api/health

> Chú ý: Các cổng trên là cổng mặc định. Nếu dự án đã cấu hình lại cổng qua `.env`, hãy truy cập theo cổng tương ứng.
