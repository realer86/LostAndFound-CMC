import React, { useEffect, useRef, useState } from 'react';
import { BadgeCheck, CheckCircle2, MessageSquareQuote, Star } from 'lucide-react';
import './roomReviewForm.css';

// Reuses the same verified-review API as the public Trust section.
// Each room is eligible only after BOTH parties confirm physical handover.
export default function RoomReviewForm({ room, autoFocus = false }) {
  const rootRef = useRef(null);
  const [eligibility, setEligibility] = useState('loading');
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    setEligibility('loading');
    setRating(5);
    setComment('');
    setError('');
    fetch(`/api/reviews/eligible?handoverRoomId=${encodeURIComponent(room.id)}`, { credentials: 'same-origin' })
      .then(response => {
        if (!response.ok) throw new Error('Không kiểm tra được quyền đánh giá.');
        return response.json();
      })
      .then(items => { if (active) setEligibility(Array.isArray(items) && items.some(item => item.handoverRoomId === room.id) ? 'eligible' : 'reviewed'); })
      .catch(() => { if (active) setEligibility('error'); });
    return () => { active = false; };
  }, [room.id]);

  // Scroll only when this user completes the final confirmation, not on every poll.
  useEffect(() => {
    if (autoFocus) rootRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }, [autoFocus]);

  async function submit(event) {
    event.preventDefault();
    if (eligibility !== 'eligible' || submitting) return;
    setSubmitting(true);
    setError('');
    try {
      const response = await fetch('/api/reviews', {
        method: 'POST', credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ handoverRoomId: room.id, rating, comment: comment.trim() }),
      });
      const data = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(data.message || 'Không gửi được đánh giá.');
      setEligibility('submitted');
      setComment('');
      // Public reviews refresh on next visit to the homepage; no fake local data.
    } catch (err) {
      if (String(err.message).includes('đã đánh giá')) setEligibility('reviewed');
      else setError(err.message);
    } finally {
      setSubmitting(false);
    }
  }

  return <section ref={rootRef} className="handover-review" aria-labelledby="handover-review-title">
    <div className="handover-review-heading">
      <div className="handover-review-icon"><MessageSquareQuote size={20} /></div>
      <div>
        <h3 id="handover-review-title">Đánh giá trải nghiệm trao trả</h3>
        <p>Vật phẩm <strong>{room.itemName || 'đã bàn giao'}</strong> đã được trả lại thành công. Chia sẻ cảm nhận của bạn để cộng đồng thêm tin tưởng.</p>
      </div>
    </div>
    {eligibility === 'loading' && <p className="handover-review-hint" role="status">Đang kiểm tra lượt đánh giá…</p>}
    {eligibility === 'error' && <p className="handover-review-error" role="alert">Không kiểm tra được quyền đánh giá. Vui lòng tải lại Phòng bàn giao.</p>}
    {eligibility === 'reviewed' && <p className="handover-review-done"><CheckCircle2 size={17}/> Bạn đã đánh giá lượt bàn giao này hoặc lượt này chưa đủ điều kiện đánh giá.</p>}
    {eligibility === 'submitted' && <p className="handover-review-done" role="status"><CheckCircle2 size={17}/> Cảm ơn bạn! Đánh giá đã được gửi và sẽ hiển thị trong mục phản hồi cộng đồng.</p>}
    {eligibility === 'eligible' && <form onSubmit={submit} className="handover-review-form">
      <div>
        <span className="handover-review-label">Bạn đánh giá lần bàn giao này thế nào?</span>
        <div className="handover-review-stars" aria-label="Chọn số sao đánh giá">
          {Array.from({ length: 5 }, (_, index) => <button key={index} type="button" aria-label={`${index + 1} sao`} aria-pressed={rating === index + 1} onClick={() => setRating(index + 1)}>
            <Star size={25} fill={index < rating ? 'currentColor' : 'none'} />
          </button>)}
          <strong>{rating}/5</strong>
        </div>
      </div>
      <label className="handover-review-label" htmlFor="room-review-comment">Nhận xét (10–500 ký tự)</label>
      <textarea id="room-review-comment" value={comment} onChange={e => setComment(e.target.value)} minLength={10} maxLength={500} rows={3} required placeholder="Ví dụ: Người nhặt hỗ trợ nhiệt tình, bàn giao đúng lịch…" />
      <div className="handover-review-bottom">
        <span><BadgeCheck size={16}/> Đánh giá gắn với lần bàn giao đã xác minh</span>
        <button className="btn primary" type="submit" disabled={submitting || comment.trim().length < 10 || comment.trim().length > 500}>{submitting ? 'Đang gửi…' : 'Gửi đánh giá'}</button>
      </div>
      {error && <p className="handover-review-error" role="alert">{error}</p>}
    </form>}
  </section>;
}
