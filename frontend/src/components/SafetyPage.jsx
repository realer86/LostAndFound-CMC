import React from 'react';
import { ShieldCheck, UserCheck, FileSearch, CalendarClock, CheckCheck, Building2, Ban, MapPinned, Flag, ArrowLeft } from 'lucide-react';
import './safetyPage.css';

// Các bước bên dưới mô tả đúng quy trình đã có trong hệ thống (claims, phòng bàn giao, ký gửi, báo cáo).
const steps = [
  { icon: UserCheck, title: 'Đăng nhập bằng email trường', text: 'Mọi người dùng đều phải xác minh email sinh viên hoặc giảng viên CMC trước khi đăng tin hay gửi yêu cầu.' },
  { icon: FileSearch, title: 'Gửi bằng chứng sở hữu', text: 'Với đồ nhặt được, người nhận phải mô tả đặc điểm mà chỉ chủ đồ biết. Nội dung này chỉ người giữ đồ và quản trị viên xem được.' },
  { icon: ShieldCheck, title: 'Người giữ đồ xem xét và duyệt', text: 'Người giữ đồ đối chiếu bằng chứng rồi duyệt hoặc từ chối. Có thể hủy chấp thuận nếu phát hiện điều bất thường, và các yêu cầu còn lại sẽ được xem xét lại.' },
  { icon: CalendarClock, title: 'Hẹn lịch trong phòng bàn giao', text: 'Phòng bàn giao chỉ mở cho hai bên sau khi duyệt. Hai bên thống nhất thời gian, địa điểm rồi mới xác nhận được.' },
  { icon: CheckCheck, title: 'Cả hai bên cùng xác nhận', text: 'Chỉ khi người giữ đồ và người nhận đều bấm xác nhận sau khi gặp mặt, vụ việc mới hoàn tất và bài đăng chuyển sang "Đã trả".' },
];

const tips = [
  { icon: MapPinned, text: 'Gặp ở nơi công khai trong khuôn viên trường, ban ngày hoặc giờ đông người.' },
  { icon: Building2, text: 'Không tiện gặp trực tiếp? Có thể gửi đồ ở quầy bảo vệ, thư viện hoặc ký túc xá qua mục Hỗ trợ & ký gửi.' },
  { icon: Ban, text: 'Không chuyển tiền, không chia sẻ mã OTP hay mật khẩu cho bất kỳ ai, kể cả người tự nhận là quản trị viên.' },
  { icon: FileSearch, text: 'Không đăng công khai số giấy tờ hay đặc điểm bí mật của đồ vật. Hãy để dành cho bước xác minh.' },
  { icon: Flag, text: 'Thấy bài đăng hoặc người dùng đáng ngờ, dùng nút "Báo cáo bài đăng vi phạm" để quản trị viên xử lý.' },
];

const faqs = [
  ['Người lạ có xem được thông tin liên hệ của tôi không?', 'Không. Phòng trao đổi chỉ hiện tên hiển thị và chỉ mở cho hai bên của một yêu cầu đã được duyệt.'],
  ['Điểm "gợi ý tương đồng" có đảm bảo đó là đồ của tôi không?', 'Không. Điểm chỉ giúp bạn tìm nhanh hơn. Quyền nhận đồ luôn dựa trên bước xác minh bằng chứng.'],
  ['Nếu có người nhận nhầm hoặc gian lận thì sao?', 'Người giữ đồ có thể hủy chấp thuận trước khi bàn giao. Bạn cũng có thể báo cáo để quản trị viên xem xét và xử lý tài khoản vi phạm.'],
  ['Tin đăng có tự hết hạn không?', 'Có. Bài hết hạn sẽ chuyển trạng thái, và chủ bài có thể gia hạn thêm 30 ngày khi vẫn cần.'],
];

export default function SafetyPage({ onBack, onSupport }) {
  return (
    <section className="container privatepage safety-page">
      <button type="button" className="safety-back" onClick={onBack}><ArrowLeft size={15} /> Về trang chủ</button>
      <span className="kicker">AN TOÀN & TIN CẬY</span>
      <h1>Quy trình trả đồ an toàn của Foundly</h1>
      <p className="safety-lead">Mỗi món đồ đi qua đúng các bước dưới đây trước khi được tính là đã trả. Không ai có thể nhận đồ chỉ bằng một cú nhấp chuột.</p>

      <ol className="safety-steps">
        {steps.map(({ icon: Icon, title, text }, i) => (
          <li key={title}>
            <span className="safety-num">{i + 1}</span>
            <span className="safety-stepicon"><Icon size={20} aria-hidden="true" /></span>
            <div><h3>{title}</h3><p>{text}</p></div>
          </li>
        ))}
      </ol>

      <h2>Mẹo giữ an toàn khi trao đổi</h2>
      <ul className="safety-tips">
        {tips.map(({ icon: Icon, text }) => (
          <li key={text}><Icon size={18} aria-hidden="true" /><span>{text}</span></li>
        ))}
      </ul>

      <h2>Câu hỏi thường gặp</h2>
      <div className="safety-faq">
        {faqs.map(([q, a]) => (
          <details key={q}><summary>{q}</summary><p>{a}</p></details>
        ))}
      </div>

      <div className="safety-cta">
        <div><strong>Cần hỗ trợ hoặc muốn báo cáo sự cố?</strong><p>Quản trị viên sẽ phản hồi qua mục Hỗ trợ & Danh bạ.</p></div>
        <button type="button" className="btn primary" onClick={onSupport}>Mở Hỗ trợ & Danh bạ</button>
      </div>
    </section>
  );
}
