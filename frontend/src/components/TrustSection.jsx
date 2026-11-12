import React, { useEffect, useState } from 'react';
import { ShieldCheck, GraduationCap, LockKeyhole, MessagesSquare, CheckCheck, Flag, ArrowRight, Star, BadgeCheck, MessageSquareQuote } from 'lucide-react';
import './trustSection.css';

// Descriptions reflect actual product controls; public feedback is never hardcoded.
const items = [
  { icon: GraduationCap, title: 'Xác minh email trường', text: 'Tài khoản đăng tin và nhận đồ cần xác minh email CMC.' },
  { icon: LockKeyhole, title: 'Bảo mật bằng chứng', text: 'Đặc điểm nhận dạng riêng tư không hiển thị trong bài đăng công khai.' },
  { icon: MessagesSquare, title: 'Trao đổi có kiểm soát', text: 'Phòng bàn giao chỉ dành cho hai bên sau khi yêu cầu nhận đồ được duyệt.' },
  { icon: CheckCheck, title: 'Hai bên cùng xác nhận', text: 'Chỉ ghi nhận trả đồ thành công sau khi cả hai bên xác nhận.' },
  { icon: Flag, title: 'Có thể báo cáo sự cố', text: 'Người dùng có thể báo cáo bài đăng và yêu cầu quản trị viên hỗ trợ.' },
];

const fmt = (n) => Number(n).toLocaleString('vi-VN');
const shortDate = (date) => new Date(date).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });

export default function TrustSection({ onSafety, me }) {
  const [stats, setStats] = useState(null);
  const [reviews, setReviews] = useState({ count: 0, average: null, items: [] });
  const [reviewLoad, setReviewLoad] = useState('loading');
  const [eligible, setEligible] = useState([]);
  const [room, setRoom] = useState('');
  const [stars, setStars] = useState(5);
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const loadReviews = () => fetch('/api/reviews/public', { credentials: 'omit' })
    .then((r) => { if (!r.ok) throw new Error('Không tải được đánh giá'); return r.json(); })
    .then((data) => { setReviews(data); setReviewLoad('ready'); })
    .catch(() => setReviewLoad('error'));

  useEffect(() => {
    let active = true;
    fetch('/api/stats/public', { credentials: 'omit' })
      .then((r) => (r.ok ? r.json() : null))
      .then((data) => { if (active && data) setStats(data); })
      .catch(() => {});
    // Independently load public reviews for visitors, including signed-out users.
    loadReviews();
    return () => { active = false; };
  }, []);

  useEffect(() => {
    let active = true;
    setEligible([]);
    setRoom('');
    if (me?.id) {
      fetch('/api/reviews/eligible', { credentials: 'same-origin' })
        .then((r) => (r.ok ? r.json() : []))
        .then((data) => { if (active) { setEligible(data); setRoom(data[0]?.handoverRoomId || ''); } })
        .catch(() => {});
    }
    return () => { active = false; };
  }, [me?.id]);

  async function submitReview(event) {
    event.preventDefault();
    setError(''); setSuccess(''); setSubmitting(true);
    try {
      const response = await fetch('/api/reviews', {
        method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ handoverRoomId: room, rating: stars, comment: comment.trim() }),
      });
      const data = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(data.message || 'Không gửi được đánh giá.');
      setSuccess(data.message || 'Đã gửi đánh giá.');
      setComment(''); setStars(5);
      const left = eligible.filter((item) => item.handoverRoomId !== room);
      setEligible(left); setRoom(left[0]?.handoverRoomId || '');
      await loadReviews();
    } catch (err) { setError(err.message); }
    finally { setSubmitting(false); }
  }

  const tiles = stats ? [
    { value: stats.returned, label: 'món đồ đã được trả lại' },
    { value: stats.open, label: 'tin đang mở' },
    { value: stats.members, label: 'thành viên xác minh email' },
  ].filter((tile) => tile.value > 0) : [];

  return (
    <section className="trust" id="trust" aria-labelledby="trust-heading">
      <div className="container">
        <div className="trust-intro">
          <div>
            <span className="kicker">AN TOÀN & TIN CẬY</span>
            <h2 id="trust-heading">Vì sao tin tưởng Foundly?</h2>
            <p className="trust-lead">Quy trình xác minh rõ ràng, bàn giao có xác nhận hai phía và đánh giá từ trải nghiệm thực tế.</p>
          </div>
          {onSafety && <button type="button" className="btn outline trust-link" onClick={onSafety}>Tìm hiểu quy trình an toàn <ArrowRight size={16}/></button>}
        </div>

        {tiles.length > 0 && <div className="trust-stats" aria-label="Số liệu thực tế của Foundly">
          {tiles.map((tile) => <div key={tile.label}><strong>{fmt(tile.value)}</strong><span>{tile.label}</span></div>)}
        </div>}

        <div className="trust-reviews" id="trust-reviews" aria-labelledby="trust-reviews-title">
          <div className="trust-reviews-head">
            <div>
              <span className="trust-tag"><MessageSquareQuote size={16}/> PHẢN HỒI CỘNG ĐỒNG</span>
              <h3 id="trust-reviews-title">Đánh giá từ những lần trả đồ thật</h3>
              <p>Ai cũng có thể đọc đánh giá, không cần đăng nhập. Chỉ thành viên đã hoàn tất bàn giao mới có thể viết nhận xét.</p>
            </div>
            {reviews.count > 0 && <div className="trust-rating-total" aria-label={`Trung bình ${reviews.average} trên 5 sao, ${reviews.count} đánh giá`}>
              <strong>{reviews.average}<Star size={25} fill="currentColor" /></strong>
              <span>{fmt(reviews.count)} đánh giá đã xác minh</span>
            </div>}
          </div>
          {reviewLoad === 'loading' && <p className="trust-empty" role="status">Đang tải đánh giá thực tế…</p>}
          {reviewLoad === 'error' && <div className="trust-empty" role="status">Chưa tải được đánh giá. <button type="button" onClick={() => { setReviewLoad('loading'); loadReviews(); }}>Thử lại</button></div>}
          {reviewLoad === 'ready' && reviews.items.length === 0 && <p className="trust-empty">Chưa có đánh giá từ các lần bàn giao hoàn tất. Các nhận xét thực tế sẽ xuất hiện tại đây khi có người chia sẻ.</p>}
          {reviewLoad === 'ready' && reviews.items.length > 0 && <div className="trust-review-grid">
            {reviews.items.map((item, index) => <article className="trust-review-card" key={`${item.createdAt}-${index}`}>
              <div className="trust-review-stars" aria-label={`${item.rating} trên 5 sao`}>
                {Array.from({ length: 5 }, (_, i) => <Star key={i} size={16} fill={i < item.rating ? 'currentColor' : 'none'} />)}
              </div>
              <p className="trust-review-comment">“{item.comment}”</p>
              <div className="trust-review-meta"><span><BadgeCheck size={16} /> Đã bàn giao thành công</span><small>{item.participantRole} · {shortDate(item.createdAt)}</small></div>
            </article>)}
          </div>}

          {me && eligible.length > 0 && <form className="trust-review-form" onSubmit={submitReview}>
            <h4>Chia sẻ trải nghiệm bàn giao của bạn</h4>
            <label>Chọn lần bàn giao đã hoàn tất
              <select value={room} onChange={(e) => setRoom(e.target.value)} required>
                {eligible.map((item) => <option key={item.handoverRoomId} value={item.handoverRoomId}>{item.itemName} · {shortDate(item.completedAt)}</option>)}
              </select>
            </label>
            <div className="trust-review-star-picker" aria-label="Đánh giá số sao">
              {Array.from({ length: 5 }, (_, i) => <button type="button" key={i} aria-label={`${i + 1} sao`} aria-pressed={stars === i + 1} onClick={() => setStars(i + 1)}><Star size={23} fill={i < stars ? 'currentColor' : 'none'}/></button>)}
            </div>
            <label>Nhận xét (10–500 ký tự)
              <textarea value={comment} minLength={10} maxLength={500} required rows={3} placeholder="Chia sẻ trải nghiệm thực tế của bạn…" onChange={(e) => setComment(e.target.value)}/>
            </label>
            <button className="btn primary" type="submit" disabled={!room || submitting}>{submitting ? 'Đang gửi…' : 'Gửi đánh giá'}</button>
            {error && <p className="trust-form-error" role="alert">{error}</p>}
          </form>}
          {success && <p className="trust-form-success" role="status">{success}</p>}
        </div>

        <div className="trust-principles-head"><h3>Những nguyên tắc bảo vệ người dùng</h3><p>Được áp dụng trong quy trình đăng tin, xác minh và bàn giao.</p></div>
        <div className="trust-grid">
          {items.map(({ icon: Icon, title, text }) => <article key={title} className="trust-card"><span className="trust-icon"><Icon size={21} aria-hidden="true"/></span><h4>{title}</h4><p>{text}</p></article>)}
        </div>
        <p className="trust-note"><ShieldCheck size={16} aria-hidden="true"/> Điểm gợi ý tương đồng chỉ để tham khảo, không thay thế bước xác minh quyền sở hữu.</p>
      </div>
    </section>
  );
}
