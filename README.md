# Foundly — Hệ thống hỗ trợ tìm kiếm đồ thất lạc

> **Lost & Found | Dự án học tập môn Công nghệ phần mềm**

## Giới thiệu

**Foundly** là dự án ứng dụng web hỗ trợ cộng đồng sinh viên đăng thông tin về đồ vật bị mất hoặc nhặt được, tìm kiếm thông tin liên quan và kết nối các bên để hỗ trợ trả lại tài sản.

Dự án hướng tới một quy trình tập trung, dễ sử dụng và có thể theo dõi, thay cho việc tìm kiếm thông tin thất lạc rải rác qua nhiều nhóm hoặc kênh liên lạc.

## Mục tiêu

- Giúp người mất đồ dễ dàng đăng tin và tìm kiếm vật phẩm.
- Hỗ trợ người nhặt được đồ cung cấp thông tin để tìm chủ sở hữu.
- Tăng khả năng kết nối các tin báo mất và tin nhặt được dựa trên thông tin vật phẩm.
- Hỗ trợ xác minh, trao đổi và cập nhật trạng thái xử lý phù hợp.
- Xây dựng sản phẩm theo quy trình phát triển phần mềm có phân công, kiểm thử và quản lý phiên bản.

## Định hướng chức năng

Foundly được phát triển từng bước theo các Sprint, hướng tới những nhóm chức năng sau:

- **Tài khoản:** Đăng ký, đăng nhập và quản lý thông tin người dùng.
- **Đăng tin:** Tạo tin báo mất hoặc nhặt được, bổ sung mô tả, vị trí, thời gian và hình ảnh.
- **Tra cứu:** Tìm kiếm và lọc tin theo thông tin vật phẩm.
- **Đối sánh:** Gợi ý các tin báo mất và nhặt được có đặc điểm tương đồng.
- **Xác minh và hoàn trả:** Hỗ trợ liên hệ, xác nhận và cập nhật tiến trình trả lại đồ.
- **Quản trị:** Hỗ trợ quản lý nội dung và xử lý các trường hợp cần kiểm tra.

> Đây là **phạm vi định hướng của sản phẩm**, không phải danh sách các chức năng đã hoàn thành ở mọi thời điểm. Trạng thái triển khai thực tế được theo dõi qua GitHub Issues và Pull Requests.

## Công nghệ sử dụng

- **Frontend:** React
- **Backend:** ASP.NET Core
- **Cơ sở dữ liệu:** PostgreSQL
- **Môi trường phát triển:** Docker
- **Quản lý mã nguồn và công việc:** Git, GitHub Issues và Pull Requests

## Tổ chức dự án

Repository tập hợp mã nguồn giao diện, backend, cơ sở dữ liệu và tài liệu phục vụ quá trình phát triển Foundly.

Công việc được chia theo từng Sprint, với các Issue mô tả yêu cầu và tiêu chí nghiệm thu; thay đổi mã nguồn hoặc tài liệu được đề xuất và kiểm tra thông qua Pull Request.

Các tài liệu chi tiết như thiết kế hệ thống, hướng dẫn chạy, kịch bản kiểm thử và báo cáo được quản lý riêng trong repository, thay vì bổ sung liên tục vào trang giới thiệu này.

## Theo dõi phát triển

- [GitHub Issues](https://github.com/realer86/LostAndFound-CMC/issues) — yêu cầu, nhiệm vụ và lỗi cần xử lý.
- [Pull Requests](https://github.com/realer86/LostAndFound-CMC/pulls) — các thay đổi đang được đề xuất, kiểm tra hoặc tích hợp.
- [Thư mục tài liệu](./docs) — tài liệu phục vụ thiết kế, triển khai và kiểm thử.

## Lưu ý

Foundly là **dự án học tập**, được xây dựng nhằm thực hành quy trình phát triển phần mềm và giải quyết bài toán đồ thất lạc trong môi trường đại học; không phải hệ thống chính thức của nhà trường.
